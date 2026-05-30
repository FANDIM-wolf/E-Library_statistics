using System;
using System.Collections.Generic;
using System.Globalization;
using ELibraryStatistics.Models;
using Microsoft.Data.Sqlite;

namespace ELibraryStatistics.Data {
  // Tracks the history of imported files so their rows can be located and
  // removed later.
  public class SourceFileRepository {
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";

    private readonly DatabaseContext _context;

    public SourceFileRepository(DatabaseContext context) {
      _context = context;
    }

    public int Count() {
      using SqliteConnection connection = _context.OpenConnection();
      using SqliteCommand command = connection.CreateCommand();
      command.CommandText = "SELECT COUNT(*) FROM source_files;";
      return (int)(long)command.ExecuteScalar()!;
    }

    public long Insert(SqliteConnection connection, SqliteTransaction transaction, string fileName,
        DateTime loadedAt) {
      using SqliteCommand command = connection.CreateCommand();
      command.Transaction = transaction;
      command.CommandText =
          "INSERT INTO source_files (file_name, loaded_at) VALUES ($name, $loaded); " +
          "SELECT last_insert_rowid();";
      command.Parameters.AddWithValue("$name", fileName);
      command.Parameters.AddWithValue("$loaded", loadedAt.ToString(TimestampFormat,
          CultureInfo.InvariantCulture));
      return (long)command.ExecuteScalar()!;
    }

    public void UpdateCounts(SqliteConnection connection, SqliteTransaction transaction, long id,
        int articleCount, int authorCount) {
      using SqliteCommand command = connection.CreateCommand();
      command.Transaction = transaction;
      command.CommandText =
          "UPDATE source_files SET article_count = $articles, author_count = $authors " +
          "WHERE id = $id;";
      command.Parameters.AddWithValue("$articles", articleCount);
      command.Parameters.AddWithValue("$authors", authorCount);
      command.Parameters.AddWithValue("$id", id);
      command.ExecuteNonQuery();
    }

    public List<SourceFileRecord> GetAll() {
      var records = new List<SourceFileRecord>();
      using SqliteConnection connection = _context.OpenConnection();
      using SqliteCommand command = connection.CreateCommand();
      command.CommandText =
          "SELECT id, file_name, loaded_at, article_count, author_count FROM source_files " +
          "ORDER BY loaded_at DESC, id DESC;";
      using SqliteDataReader reader = command.ExecuteReader();
      while (reader.Read()) {
        records.Add(ReadRecord(reader));
      }

      return records;
    }

    public SourceFileRecord? GetById(long id) {
      using SqliteConnection connection = _context.OpenConnection();
      using SqliteCommand command = connection.CreateCommand();
      command.CommandText =
          "SELECT id, file_name, loaded_at, article_count, author_count FROM source_files " +
          "WHERE id = $id;";
      command.Parameters.AddWithValue("$id", id);
      using SqliteDataReader reader = command.ExecuteReader();
      return reader.Read() ? ReadRecord(reader) : null;
    }

    public void Delete(SqliteConnection connection, SqliteTransaction transaction, long id) {
      using SqliteCommand command = connection.CreateCommand();
      command.Transaction = transaction;
      command.CommandText = "DELETE FROM source_files WHERE id = $id;";
      command.Parameters.AddWithValue("$id", id);
      command.ExecuteNonQuery();
    }

    private static SourceFileRecord ReadRecord(SqliteDataReader reader) {
      return new SourceFileRecord {
        Id = reader.GetInt64(0),
        FileName = reader.GetString(1),
        LoadedAt = DateTime.ParseExact(reader.GetString(2), TimestampFormat,
            CultureInfo.InvariantCulture),
        ArticleCount = reader.GetInt32(3),
        AuthorCount = reader.GetInt32(4),
      };
    }
  }
}
