using System;

namespace ELibraryStatistics.Models {
  // One row of the file upload history shown on the statistics page. The
  // DisplayName follows the "<name>_<id>" convention required for source_file.
  public class SourceFileRecord {
    public long Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public DateTime LoadedAt { get; set; }

    public int ArticleCount { get; set; }

    public int AuthorCount { get; set; }

    public string DisplayName => $"{FileName}_{Id}";
  }
}
