using System.Collections.Generic;
using ELibraryStatistics.Models;

namespace ELibraryStatistics.Services {
  // Carries the parsed articles and the computed preview between the "inspect"
  // and "commit" phases of an import, so the workbook is read only once.
  public class ImportSession {
    public string FilePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public IReadOnlyList<Article> Articles { get; set; } = new List<Article>();

    public ImportPreview Preview { get; set; } = new();
  }
}
