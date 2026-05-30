using System.Collections.Generic;

namespace ELibraryStatistics.Models {
  // The full information shown after an author is selected from search
  // results: the author plus every article linked to their AuthorId.
  public class AuthorDetails {
    public Author Author { get; set; } = new();

    public List<Article> Articles { get; } = new();
  }
}
