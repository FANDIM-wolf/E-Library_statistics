using Microsoft.Data.Sqlite;

namespace ELibraryStatistics.Data {
  // Owns the SQLite connection string and creates the schema on first use.
  // A short-lived connection is opened per operation; SQLite handles this
  // efficiently and it keeps the repositories simple and thread-friendly.
  public class DatabaseContext {
    private readonly string _connectionString;

    public DatabaseContext(string databasePath) {
      _connectionString = new SqliteConnectionStringBuilder {
        DataSource = databasePath,
        Mode = SqliteOpenMode.ReadWriteCreate,
      }.ToString();
    }

    public SqliteConnection OpenConnection() {
      var connection = new SqliteConnection(_connectionString);
      connection.Open();

      // Foreign keys are off by default in SQLite and must be enabled per
      // connection so that cascading deletes work when a file is removed.
      using SqliteCommand pragma = connection.CreateCommand();
      pragma.CommandText = "PRAGMA foreign_keys = ON;";
      pragma.ExecuteNonQuery();

      return connection;
    }

    public void Initialize() {
      using SqliteConnection connection = OpenConnection();
      using SqliteCommand command = connection.CreateCommand();
      command.CommandText = SchemaSql;
      command.ExecuteNonQuery();
    }

    private const string SchemaSql = @"
CREATE TABLE IF NOT EXISTS source_files (
  id            INTEGER PRIMARY KEY AUTOINCREMENT,
  file_name     TEXT NOT NULL,
  loaded_at     TEXT NOT NULL,
  article_count INTEGER NOT NULL DEFAULT 0,
  author_count  INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS authors (
  authorid INTEGER PRIMARY KEY,
  lastname TEXT NOT NULL DEFAULT '',
  initials TEXT NOT NULL DEFAULT '',
  email    TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS articles (
  id            INTEGER PRIMARY KEY,
  title         TEXT NOT NULL DEFAULT '',
  yearpubl      INTEGER,
  publisher     TEXT,
  issn          TEXT,
  isbn          TEXT,
  doi           TEXT,
  pages         TEXT,
  volume        TEXT,
  number        TEXT,
  vak           TEXT,
  rsci          TEXT,
  wos           TEXT,
  scopus        TEXT,
  white_list    TEXT,
  doaj          TEXT,
  corerisc      TEXT,
  cited         INTEGER,
  citation      TEXT,
  confname      TEXT,
  confplace     TEXT,
  confdatebegin TEXT,
  confdateend   TEXT,
  abstract      TEXT,
  supported     TEXT,
  reference     TEXT,
  genre         TEXT,
  type          TEXT,
  source_file   TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS article_authors (
  article_id INTEGER NOT NULL REFERENCES articles(id) ON DELETE CASCADE,
  authorid   INTEGER NOT NULL REFERENCES authors(authorid) ON DELETE CASCADE,
  PRIMARY KEY (article_id, authorid)
);

CREATE TABLE IF NOT EXISTS keywords (
  article_id INTEGER NOT NULL REFERENCES articles(id) ON DELETE CASCADE,
  keyword    TEXT NOT NULL,
  lang       TEXT
);

CREATE INDEX IF NOT EXISTS ix_articles_title ON articles(title);
CREATE INDEX IF NOT EXISTS ix_articles_source ON articles(source_file);
CREATE INDEX IF NOT EXISTS ix_authors_lastname ON authors(lastname);
CREATE INDEX IF NOT EXISTS ix_article_authors_author ON article_authors(authorid);
";
  }
}
