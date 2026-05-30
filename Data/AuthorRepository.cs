using System.Collections.Generic;
using ELibraryStatistics.Models;
using Microsoft.Data.Sqlite;

namespace ELibraryStatistics.Data {
  // Read and write access to the authors table. Write methods accept an active
  // connection and transaction so that an import inserts articles, authors and
  // links atomically.
  public class AuthorRepository {
    private readonly DatabaseContext _context;

    public AuthorRepository(DatabaseContext context) {
      _context = context;
    }

    public int Count() {
      using SqliteConnection connection = _context.OpenConnection();
      using SqliteCommand command = connection.CreateCommand();
      command.CommandText = "SELECT COUNT(*) FROM authors;";
      return (int)(long)command.ExecuteScalar()!;
    }

    public HashSet<long> GetExistingIds() {
      var ids = new HashSet<long>();
      using SqliteConnection connection = _context.OpenConnection();
      using SqliteCommand command = connection.CreateCommand();
      command.CommandText = "SELECT authorid FROM authors;";
      using SqliteDataReader reader = command.ExecuteReader();
      while (reader.Read()) {
        ids.Add(reader.GetInt64(0));
      }

      return ids;
    }

    // Inserts the author if no row with the same AuthorId exists yet. Duplicate
    // authors are silently ignored, which is the required deduplication rule.
    public void InsertIfMissing(SqliteConnection connection, SqliteTransaction transaction,
        Author author) {
      using SqliteCommand command = connection.CreateCommand();
      command.Transaction = transaction;
      command.CommandText =
          "INSERT OR IGNORE INTO authors (authorid, lastname, initials, email) " +
          "VALUES ($id, $lastname, $initials, $email);";
      command.Parameters.AddWithValue("$id", author.AuthorId);
      command.Parameters.AddWithValue("$lastname", author.LastName);
      command.Parameters.AddWithValue("$initials", author.Initials);
      command.Parameters.AddWithValue("$email", author.Email);
      command.ExecuteNonQuery();
    }

    public List<Author> SearchByLastName(string lastNamePart) {
      var authors = new List<Author>();
      using SqliteConnection connection = _context.OpenConnection();
      using SqliteCommand command = connection.CreateCommand();
      command.CommandText =
          "SELECT authorid, lastname, initials, email FROM authors " +
          "WHERE lastname LIKE $pattern ORDER BY lastname, initials;";
      command.Parameters.AddWithValue("$pattern", "%" + lastNamePart + "%");
      using SqliteDataReader reader = command.ExecuteReader();
      while (reader.Read()) {
        authors.Add(ReadAuthor(reader));
      }

      return authors;
    }

    public Author? GetById(long authorId) {
      using SqliteConnection connection = _context.OpenConnection();
      using SqliteCommand command = connection.CreateCommand();
      command.CommandText =
          "SELECT authorid, lastname, initials, email FROM authors WHERE authorid = $id;";
      command.Parameters.AddWithValue("$id", authorId);
      using SqliteDataReader reader = command.ExecuteReader();
      return reader.Read() ? ReadAuthor(reader) : null;
    }

    // Removes authors that are no longer linked to any article. This runs after
    // a file is deleted so orphaned author rows do not inflate the statistics.
    public void DeleteOrphans(SqliteConnection connection, SqliteTransaction transaction) {
      using SqliteCommand command = connection.CreateCommand();
      command.Transaction = transaction;
      command.CommandText =
          "DELETE FROM authors WHERE authorid NOT IN " +
          "(SELECT authorid FROM article_authors);";
      command.ExecuteNonQuery();
    }

    private static Author ReadAuthor(SqliteDataReader reader) {
      return new Author {
        AuthorId = reader.GetInt64(0),
        LastName = reader.GetString(1),
        Initials = reader.GetString(2),
        Email = reader.GetString(3),
      };
    }
  }
}
