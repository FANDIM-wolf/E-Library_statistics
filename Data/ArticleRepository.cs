using System.Collections.Generic;
using System.Text;
using ELibraryStatistics.Models;
using Microsoft.Data.Sqlite;

namespace ELibraryStatistics.Data {
  // Read and write access to the articles table together with its keyword and
  // author link rows.
  public class ArticleRepository {
    // The article columns in a fixed order, reused by both INSERT and SELECT so
    // the reader offsets always line up with the schema.
    private const string Columns =
        "id, title, yearpubl, publisher, issn, isbn, doi, pages, volume, number, " +
        "vak, rsci, wos, scopus, white_list, doaj, corerisc, cited, citation, " +
        "confname, confplace, confdatebegin, confdateend, abstract, supported, " +
        "reference, genre, type, source_file";

    private readonly DatabaseContext _context;

    public ArticleRepository(DatabaseContext context) {
      _context = context;
    }

    public int Count() {
      using SqliteConnection connection = _context.OpenConnection();
      using SqliteCommand command = connection.CreateCommand();
      command.CommandText = "SELECT COUNT(*) FROM articles;";
      return (int)(long)command.ExecuteScalar()!;
    }

    public HashSet<long> GetExistingIds() {
      var ids = new HashSet<long>();
      using SqliteConnection connection = _context.OpenConnection();
      using SqliteCommand command = connection.CreateCommand();
      command.CommandText = "SELECT id FROM articles;";
      using SqliteDataReader reader = command.ExecuteReader();
      while (reader.Read()) {
        ids.Add(reader.GetInt64(0));
      }

      return ids;
    }

    // Inserts the article and its keyword/author links. The caller guarantees
    // the id is new, so a plain INSERT is used inside the import transaction.
    public void Insert(SqliteConnection connection, SqliteTransaction transaction,
        Article article) {
      using (SqliteCommand command = connection.CreateCommand()) {
        command.Transaction = transaction;
        command.CommandText =
            $"INSERT INTO articles ({Columns}) VALUES (" +
            "$id, $title, $yearpubl, $publisher, $issn, $isbn, $doi, $pages, $volume, " +
            "$number, $vak, $rsci, $wos, $scopus, $white_list, $doaj, $corerisc, $cited, " +
            "$citation, $confname, $confplace, $confdatebegin, $confdateend, $abstract, " +
            "$supported, $reference, $genre, $type, $source_file);";
        AddArticleParameters(command, article);
        command.ExecuteNonQuery();
      }

      foreach (Author author in article.Authors) {
        using SqliteCommand link = connection.CreateCommand();
        link.Transaction = transaction;
        link.CommandText =
            "INSERT OR IGNORE INTO article_authors (article_id, authorid) " +
            "VALUES ($article, $author);";
        link.Parameters.AddWithValue("$article", article.Id);
        link.Parameters.AddWithValue("$author", author.AuthorId);
        link.ExecuteNonQuery();
      }

      foreach (string keyword in article.Keywords) {
        using SqliteCommand kw = connection.CreateCommand();
        kw.Transaction = transaction;
        kw.CommandText =
            "INSERT INTO keywords (article_id, keyword, lang) VALUES ($article, $keyword, NULL);";
        kw.Parameters.AddWithValue("$article", article.Id);
        kw.Parameters.AddWithValue("$keyword", keyword);
        kw.ExecuteNonQuery();
      }
    }

    public List<Article> Search(ArticleSearchCriteria criteria) {
      var sql = new StringBuilder($"SELECT {Columns} FROM articles WHERE 1 = 1");
      if (!string.IsNullOrWhiteSpace(criteria.TitleContains)) {
        sql.Append(" AND title LIKE $title");
      }

      if (criteria.Year.HasValue) {
        sql.Append(" AND yearpubl = $year");
      }

      if (criteria.CoreRisc.HasValue) {
        sql.Append(" AND LOWER(corerisc) = $corerisc");
      }

      sql.Append(" ORDER BY title LIMIT 500;");

      var articles = new List<Article>();
      using SqliteConnection connection = _context.OpenConnection();
      using SqliteCommand command = connection.CreateCommand();
      command.CommandText = sql.ToString();
      if (!string.IsNullOrWhiteSpace(criteria.TitleContains)) {
        command.Parameters.AddWithValue("$title", "%" + criteria.TitleContains + "%");
      }

      if (criteria.Year.HasValue) {
        command.Parameters.AddWithValue("$year", criteria.Year.Value);
      }

      if (criteria.CoreRisc.HasValue) {
        command.Parameters.AddWithValue("$corerisc", criteria.CoreRisc.Value ? "yes" : "no");
      }

      using SqliteDataReader reader = command.ExecuteReader();
      while (reader.Read()) {
        articles.Add(ReadArticle(reader));
      }

      return articles;
    }

    public Article? GetById(long id) {
      using SqliteConnection connection = _context.OpenConnection();
      Article? article;
      using (SqliteCommand command = connection.CreateCommand()) {
        command.CommandText = $"SELECT {Columns} FROM articles WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        using SqliteDataReader reader = command.ExecuteReader();
        article = reader.Read() ? ReadArticle(reader) : null;
      }

      if (article != null) {
        LoadAuthors(connection, article);
        LoadKeywords(connection, article);
      }

      return article;
    }

    public List<Article> GetByAuthorId(long authorId) {
      var articles = new List<Article>();
      using SqliteConnection connection = _context.OpenConnection();
      using SqliteCommand command = connection.CreateCommand();
      command.CommandText =
          $"SELECT {Columns} FROM articles WHERE id IN " +
          "(SELECT article_id FROM article_authors WHERE authorid = $author) ORDER BY title;";
      command.Parameters.AddWithValue("$author", authorId);
      using SqliteDataReader reader = command.ExecuteReader();
      while (reader.Read()) {
        articles.Add(ReadArticle(reader));
      }

      return articles;
    }

    // Deletes every article belonging to the given source_file value. Cascading
    // foreign keys remove the linked author and keyword rows automatically.
    public void DeleteBySourceFile(SqliteConnection connection, SqliteTransaction transaction,
        string sourceFile) {
      using SqliteCommand command = connection.CreateCommand();
      command.Transaction = transaction;
      command.CommandText = "DELETE FROM articles WHERE source_file = $source;";
      command.Parameters.AddWithValue("$source", sourceFile);
      command.ExecuteNonQuery();
    }

    private void LoadAuthors(SqliteConnection connection, Article article) {
      using SqliteCommand command = connection.CreateCommand();
      command.CommandText =
          "SELECT a.authorid, a.lastname, a.initials, a.email FROM authors a " +
          "JOIN article_authors la ON la.authorid = a.authorid " +
          "WHERE la.article_id = $id ORDER BY a.lastname;";
      command.Parameters.AddWithValue("$id", article.Id);
      using SqliteDataReader reader = command.ExecuteReader();
      while (reader.Read()) {
        article.Authors.Add(new Author {
          AuthorId = reader.GetInt64(0),
          LastName = reader.GetString(1),
          Initials = reader.GetString(2),
          Email = reader.GetString(3),
        });
      }
    }

    private void LoadKeywords(SqliteConnection connection, Article article) {
      using SqliteCommand command = connection.CreateCommand();
      command.CommandText = "SELECT keyword FROM keywords WHERE article_id = $id;";
      command.Parameters.AddWithValue("$id", article.Id);
      using SqliteDataReader reader = command.ExecuteReader();
      while (reader.Read()) {
        article.Keywords.Add(reader.GetString(0));
      }
    }

    private static void AddArticleParameters(SqliteCommand command, Article article) {
      command.Parameters.AddWithValue("$id", article.Id);
      command.Parameters.AddWithValue("$title", article.Title);
      command.Parameters.AddWithValue("$yearpubl", (object?)article.YearPubl ?? System.DBNull.Value);
      command.Parameters.AddWithValue("$publisher", AsDb(article.Publisher));
      command.Parameters.AddWithValue("$issn", AsDb(article.Issn));
      command.Parameters.AddWithValue("$isbn", AsDb(article.Isbn));
      command.Parameters.AddWithValue("$doi", AsDb(article.Doi));
      command.Parameters.AddWithValue("$pages", AsDb(article.Pages));
      command.Parameters.AddWithValue("$volume", AsDb(article.Volume));
      command.Parameters.AddWithValue("$number", AsDb(article.Number));
      command.Parameters.AddWithValue("$vak", AsDb(article.Vak));
      command.Parameters.AddWithValue("$rsci", AsDb(article.Rsci));
      command.Parameters.AddWithValue("$wos", AsDb(article.Wos));
      command.Parameters.AddWithValue("$scopus", AsDb(article.Scopus));
      command.Parameters.AddWithValue("$white_list", AsDb(article.WhiteList));
      command.Parameters.AddWithValue("$doaj", AsDb(article.Doaj));
      command.Parameters.AddWithValue("$corerisc", AsDb(article.CoreRisc));
      command.Parameters.AddWithValue("$cited", (object?)article.Cited ?? System.DBNull.Value);
      command.Parameters.AddWithValue("$citation", AsDb(article.Citation));
      command.Parameters.AddWithValue("$confname", AsDb(article.ConfName));
      command.Parameters.AddWithValue("$confplace", AsDb(article.ConfPlace));
      command.Parameters.AddWithValue("$confdatebegin", AsDb(article.ConfDateBegin));
      command.Parameters.AddWithValue("$confdateend", AsDb(article.ConfDateEnd));
      command.Parameters.AddWithValue("$abstract", AsDb(article.Abstract));
      command.Parameters.AddWithValue("$supported", AsDb(article.Supported));
      command.Parameters.AddWithValue("$reference", AsDb(article.Reference));
      command.Parameters.AddWithValue("$genre", AsDb(article.Genre));
      command.Parameters.AddWithValue("$type", AsDb(article.Type));
      command.Parameters.AddWithValue("$source_file", article.SourceFile);
    }

    private static object AsDb(string? value) {
      return string.IsNullOrEmpty(value) ? System.DBNull.Value : value;
    }

    private static Article ReadArticle(SqliteDataReader reader) {
      return new Article {
        Id = reader.GetInt64(0),
        Title = reader.GetString(1),
        YearPubl = reader.IsDBNull(2) ? null : reader.GetInt32(2),
        Publisher = ReadString(reader, 3),
        Issn = ReadString(reader, 4),
        Isbn = ReadString(reader, 5),
        Doi = ReadString(reader, 6),
        Pages = ReadString(reader, 7),
        Volume = ReadString(reader, 8),
        Number = ReadString(reader, 9),
        Vak = ReadString(reader, 10),
        Rsci = ReadString(reader, 11),
        Wos = ReadString(reader, 12),
        Scopus = ReadString(reader, 13),
        WhiteList = ReadString(reader, 14),
        Doaj = ReadString(reader, 15),
        CoreRisc = ReadString(reader, 16),
        Cited = reader.IsDBNull(17) ? null : reader.GetInt32(17),
        Citation = ReadString(reader, 18),
        ConfName = ReadString(reader, 19),
        ConfPlace = ReadString(reader, 20),
        ConfDateBegin = ReadString(reader, 21),
        ConfDateEnd = ReadString(reader, 22),
        Abstract = ReadString(reader, 23),
        Supported = ReadString(reader, 24),
        Reference = ReadString(reader, 25),
        Genre = ReadString(reader, 26),
        Type = ReadString(reader, 27),
        SourceFile = reader.GetString(28),
      };
    }

    private static string? ReadString(SqliteDataReader reader, int ordinal) {
      return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
  }
}
