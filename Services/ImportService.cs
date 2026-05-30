using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ELibraryStatistics.Data;
using ELibraryStatistics.Models;
using Microsoft.Data.Sqlite;

namespace ELibraryStatistics.Services {
  // Coordinates importing an export file into the database. It first reports an
  // exact preview (how many articles and authors, and how many are new) and
  // then commits only the new rows in a single transaction, so duplicate
  // articles and duplicate authors are never stored twice.
  public class ImportService {
    private readonly DatabaseContext _context;
    private readonly ArticleRepository _articles;
    private readonly AuthorRepository _authors;
    private readonly SourceFileRepository _sourceFiles;
    private readonly ExcelImportService _excel = new();

    public ImportService(DatabaseContext context, ArticleRepository articles,
        AuthorRepository authors, SourceFileRepository sourceFiles) {
      _context = context;
      _articles = articles;
      _authors = authors;
      _sourceFiles = sourceFiles;
    }

    // Reads the workbook and compares it against the database without writing
    // anything. The returned session is then passed to Commit.
    public ImportSession Prepare(string filePath) {
      IReadOnlyList<Article> parsed = _excel.Parse(filePath);
      HashSet<long> existingArticleIds = _articles.GetExistingIds();
      HashSet<long> existingAuthorIds = _authors.GetExistingIds();

      HashSet<long> fileAuthorIds = parsed
          .SelectMany(article => article.Authors)
          .Select(author => author.AuthorId)
          .ToHashSet();

      int newArticles = parsed.Count(article => !existingArticleIds.Contains(article.Id));

      var preview = new ImportPreview {
        FileName = Path.GetFileName(filePath),
        TotalArticles = parsed.Count,
        TotalAuthors = fileAuthorIds.Count,
        NewArticles = newArticles,
        DuplicateArticles = parsed.Count - newArticles,
        NewAuthors = fileAuthorIds.Count(id => !existingAuthorIds.Contains(id)),
      };

      return new ImportSession {
        FilePath = filePath,
        FileName = preview.FileName,
        Articles = parsed,
        Preview = preview,
      };
    }

    // Writes the new articles and authors of a prepared session. Returns the
    // file record that was created so the UI can refresh the history list.
    public SourceFileRecord Commit(ImportSession session) {
      HashSet<long> existingArticleIds = _articles.GetExistingIds();
      HashSet<long> existingAuthorIds = _authors.GetExistingIds();

      using SqliteConnection connection = OpenConnection();
      using SqliteTransaction transaction = connection.BeginTransaction();

      long fileId = _sourceFiles.Insert(connection, transaction, session.FileName, DateTime.Now);
      string sourceFile = $"{session.FileName}_{fileId}";

      var addedAuthorIds = new HashSet<long>();
      int addedArticles = 0;

      foreach (Article article in session.Articles) {
        if (existingArticleIds.Contains(article.Id)) {
          continue;
        }

        article.SourceFile = sourceFile;
        foreach (Author author in article.Authors) {
          _authors.InsertIfMissing(connection, transaction, author);
          if (!existingAuthorIds.Contains(author.AuthorId)) {
            addedAuthorIds.Add(author.AuthorId);
          }
        }

        _articles.Insert(connection, transaction, article);
        addedArticles++;
      }

      _sourceFiles.UpdateCounts(connection, transaction, fileId, addedArticles,
          addedAuthorIds.Count);
      transaction.Commit();

      return new SourceFileRecord {
        Id = fileId,
        FileName = session.FileName,
        LoadedAt = DateTime.Now,
        ArticleCount = addedArticles,
        AuthorCount = addedAuthorIds.Count,
      };
    }

    // Removes every article that was imported from the given file, then prunes
    // authors that are no longer referenced by any article.
    public void DeleteFile(SourceFileRecord record) {
      using SqliteConnection connection = OpenConnection();
      using SqliteTransaction transaction = connection.BeginTransaction();

      _articles.DeleteBySourceFile(connection, transaction, record.DisplayName);
      _authors.DeleteOrphans(connection, transaction);
      _sourceFiles.Delete(connection, transaction, record.Id);

      transaction.Commit();
    }

    private SqliteConnection OpenConnection() {
      return _context.OpenConnection();
    }
  }
}
