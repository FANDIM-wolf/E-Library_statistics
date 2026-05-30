namespace ELibraryStatistics.Models {
  // The filters offered on the article search page. Any combination may be
  // left empty, in which case the corresponding filter is ignored.
  public class ArticleSearchCriteria {
    public string? TitleContains { get; set; }

    public int? Year { get; set; }

    // Tri-state filter for the "corerisc" indexing flag: null means "do not
    // filter", true means only core RSCI, false means only non-core.
    public bool? CoreRisc { get; set; }
  }
}
