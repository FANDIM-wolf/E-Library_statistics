using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ELibraryStatistics.Models;

namespace ELibraryStatistics.Services {
  // Produces the user-facing export files: per-author and per-article CSV, and
  // the database statistics report as a Word document.
  public class ExportService {
    // Writes a single article and its authors/keywords as a two-column CSV
    // (field name, value) which opens cleanly in spreadsheet tools.
    public void ExportArticleToCsv(Article article, string filePath) {
      var rows = new List<(string Field, string? Value)> {
        ("id", article.Id.ToString(CultureInfo.InvariantCulture)),
        ("title", article.Title),
        ("yearpubl", article.YearPubl?.ToString(CultureInfo.InvariantCulture)),
        ("publisher", article.Publisher),
        ("issn", article.Issn),
        ("isbn", article.Isbn),
        ("doi", article.Doi),
        ("pages", article.Pages),
        ("volume", article.Volume),
        ("number", article.Number),
        ("vak", article.Vak),
        ("rsci", article.Rsci),
        ("wos", article.Wos),
        ("scopus", article.Scopus),
        ("white_list", article.WhiteList),
        ("doaj", article.Doaj),
        ("corerisc", article.CoreRisc),
        ("cited", article.Cited?.ToString(CultureInfo.InvariantCulture)),
        ("citation", article.Citation),
        ("confname", article.ConfName),
        ("confplace", article.ConfPlace),
        ("confdatebegin", article.ConfDateBegin),
        ("confdateend", article.ConfDateEnd),
        ("genre", article.Genre),
        ("type", article.Type),
        ("abstract", article.Abstract),
        ("authors", string.Join("; ", article.Authors.Select(a => $"{a.LastName} {a.Initials}"))),
        ("keywords", string.Join("; ", article.Keywords)),
        ("source_file", article.SourceFile),
      };

      var builder = new StringBuilder();
      builder.AppendLine("field,value");
      foreach ((string field, string? value) in rows) {
        builder.Append(Escape(field)).Append(',').AppendLine(Escape(value));
      }

      WriteText(filePath, builder.ToString());
    }

    // Writes an author and every article linked to their AuthorId as a CSV
    // table.
    public void ExportAuthorToCsv(AuthorDetails details, string filePath) {
      var builder = new StringBuilder();
      builder.Append("authorid,lastname,initials,email,article_id,article_title,yearpubl")
          .AppendLine();

      Author author = details.Author;
      string head =
          $"{Escape(author.AuthorId.ToString(CultureInfo.InvariantCulture))}," +
          $"{Escape(author.LastName)},{Escape(author.Initials)},{Escape(author.Email)},";

      if (details.Articles.Count == 0) {
        builder.Append(head).AppendLine(",,");
      }

      foreach (Article article in details.Articles) {
        builder.Append(head)
            .Append(Escape(article.Id.ToString(CultureInfo.InvariantCulture))).Append(',')
            .Append(Escape(article.Title)).Append(',')
            .AppendLine(Escape(article.YearPubl?.ToString(CultureInfo.InvariantCulture)));
      }

      WriteText(filePath, builder.ToString());
    }

    // Writes the aggregate statistics and the file upload history as a Word
    // (.docx) document.
    public void ExportStatisticsToDoc(DatabaseStatistics statistics,
        IReadOnlyList<SourceFileRecord> history, string version, string filePath) {
      using WordprocessingDocument document =
          WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
      MainDocumentPart mainPart = document.AddMainDocumentPart();
      mainPart.Document = new Document();
      var body = mainPart.Document.AppendChild(new Body());

      body.AppendChild(Heading("Статистика E-library"));
      body.AppendChild(Paragraph($"Дата выгрузки: {DateTime.Now:yyyy-MM-dd HH:mm}"));
      body.AppendChild(Paragraph($"Текущая версия: {version}"));
      body.AppendChild(Paragraph(string.Empty));

      body.AppendChild(Paragraph($"Количество файлов в БД: {statistics.FileCount}"));
      body.AppendChild(Paragraph($"Количество авторов в БД: {statistics.AuthorCount}"));
      body.AppendChild(Paragraph($"Количество статей в БД: {statistics.ArticleCount}"));
      body.AppendChild(Paragraph(string.Empty));

      body.AppendChild(Heading("История загрузки файлов"));
      foreach (SourceFileRecord record in history) {
        string line =
            $"{record.DisplayName} — {record.LoadedAt:yyyy-MM-dd} " +
            $"(статей: {record.ArticleCount}, авторов: {record.AuthorCount})";
        body.AppendChild(Paragraph(line));
      }

      mainPart.Document.Save();
    }

    private static Paragraph Heading(string text) {
      var run = new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
      run.RunProperties = new RunProperties(new Bold());
      return new Paragraph(run);
    }

    private static Paragraph Paragraph(string text) {
      return new Paragraph(new Run(new Text(text) {
        Space = SpaceProcessingModeValues.Preserve,
      }));
    }

    private static void WriteText(string filePath, string content) {
      // UTF-8 with BOM so that Cyrillic text is recognised by spreadsheet tools.
      File.WriteAllText(filePath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string Escape(string? value) {
      if (string.IsNullOrEmpty(value)) {
        return string.Empty;
      }

      bool needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n')
          || value.Contains('\r');
      string escaped = value.Replace("\"", "\"\"");
      return needsQuotes ? $"\"{escaped}\"" : escaped;
    }
  }
}
