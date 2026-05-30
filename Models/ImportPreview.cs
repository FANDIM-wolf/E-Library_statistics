namespace ELibraryStatistics.Models {
  // The result of inspecting an export file before it is written to the
  // database. It reports the exact number of articles and authors as well as
  // how many of them are new, so duplicates are never inserted twice.
  public class ImportPreview {
    public string FileName { get; set; } = string.Empty;

    public int TotalArticles { get; set; }

    public int TotalAuthors { get; set; }

    public int NewArticles { get; set; }

    public int DuplicateArticles { get; set; }

    public int NewAuthors { get; set; }
  }
}
