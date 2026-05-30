namespace ELibraryStatistics.Models {
  // A unique author as stored in the database. Authors are deduplicated by
  // AuthorId because the elibrary export may spell the initials differently
  // for the same person across articles.
  public class Author {
    public long AuthorId { get; set; }

    public string LastName { get; set; } = string.Empty;

    public string Initials { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
  }
}
