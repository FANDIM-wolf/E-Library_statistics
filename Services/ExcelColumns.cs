using System.Collections.Generic;

namespace ELibraryStatistics.Services {
  // Maps the logical fields used by the application to the physical header
  // names found in the elibrary export. The export repeats some logical
  // columns under suffixed names (for example "yearpubl" and "yearpubl9"), so
  // each field lists every header that may carry its value, in priority order.
  internal static class ExcelColumns {
    public const string Id = "id";
    public const string AuthorId = "authorid";
    public const string LastName = "lastname";
    public const string Initials = "initials";
    public const string Email = "email";
    public const string Keyword = "keyword";

    // Logical field name -> candidate header names. The first header that holds
    // a non-empty value wins when an article spans several rows.
    public static readonly IReadOnlyDictionary<string, string[]> ArticleFields =
        new Dictionary<string, string[]> {
          ["title"] = new[] { "title", "title4", "title5" },
          ["yearpubl"] = new[] { "yearpubl", "year", "yearpubl9" },
          ["publisher"] = new[] { "publisher" },
          ["issn"] = new[] { "issn", "eissn" },
          ["isbn"] = new[] { "isbn", "isbn10" },
          ["doi"] = new[] { "doi", "doi17" },
          ["pages"] = new[] { "pages", "pagesnumber", "pagesnumber12" },
          ["volume"] = new[] { "volume", "volumenumber" },
          ["number"] = new[] { "number", "contnumber" },
          ["vak"] = new[] { "vak" },
          ["rsci"] = new[] { "rsci", "risc" },
          ["wos"] = new[] { "wos" },
          ["scopus"] = new[] { "scopus" },
          ["white_list"] = new[] { "white_list" },
          ["doaj"] = new[] { "doaj" },
          ["corerisc"] = new[] { "corerisc" },
          ["cited"] = new[] { "cited" },
          ["citation"] = new[] { "citation" },
          ["confname"] = new[] { "confname", "confname13" },
          ["confplace"] = new[] { "confplace", "confplace14" },
          ["confdatebegin"] = new[] { "confdatebegin", "confdatebegin15" },
          ["confdateend"] = new[] { "confdateend", "confdateend16" },
          ["abstract"] = new[] { "abstract" },
          ["supported"] = new[] { "supported" },
          ["reference"] = new[] { "reference" },
          ["genre"] = new[] { "genre" },
          ["type"] = new[] { "type", "type24" },
        };
  }
}
