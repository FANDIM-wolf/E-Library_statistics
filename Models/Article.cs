using System.Collections.Generic;

namespace ELibraryStatistics.Models {
  // A unique publication keyed by its elibrary identifier. The fields mirror
  // the key column groups described in the requirements; values are taken from
  // the first non-empty cell found across the article's rows in the export.
  public class Article {
    public long Id { get; set; }

    // Bibliographic data.
    public string Title { get; set; } = string.Empty;
    public int? YearPubl { get; set; }
    public string? Publisher { get; set; }
    public string? Issn { get; set; }
    public string? Isbn { get; set; }
    public string? Doi { get; set; }
    public string? Pages { get; set; }
    public string? Volume { get; set; }
    public string? Number { get; set; }

    // Indexing data.
    public string? Vak { get; set; }
    public string? Rsci { get; set; }
    public string? Wos { get; set; }
    public string? Scopus { get; set; }
    public string? WhiteList { get; set; }
    public string? Doaj { get; set; }
    public string? CoreRisc { get; set; }
    public int? Cited { get; set; }
    public string? Citation { get; set; }

    // Conference data.
    public string? ConfName { get; set; }
    public string? ConfPlace { get; set; }
    public string? ConfDateBegin { get; set; }
    public string? ConfDateEnd { get; set; }

    // Additional data.
    public string? Abstract { get; set; }
    public string? Supported { get; set; }
    public string? Reference { get; set; }
    public string? Genre { get; set; }
    public string? Type { get; set; }

    // Provenance: "<file name>_<file id>" so a file's rows can be located and
    // removed later.
    public string SourceFile { get; set; } = string.Empty;

    public List<Author> Authors { get; } = new();

    public List<string> Keywords { get; } = new();
  }
}
