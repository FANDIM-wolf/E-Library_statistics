using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClosedXML.Excel;
using ELibraryStatistics.Models;

namespace ELibraryStatistics.Services {
  // Reads a denormalized elibrary export and rebuilds the unique articles it
  // contains. Each article spans several rows in the sheet (one bibliographic
  // row plus a row per author and per keyword), so rows are grouped by the
  // article id and the fields are merged.
  public class ExcelImportService {
    public IReadOnlyList<Article> Parse(string filePath) {
      using var workbook = new XLWorkbook(filePath);
      IXLWorksheet worksheet = workbook.Worksheets.First();
      IXLRangeRow headerRow = worksheet.RangeUsed()!.FirstRow();
      Dictionary<string, int> headerColumns = MapHeaders(headerRow);

      Dictionary<string, int[]> fieldColumns = ExcelColumns.ArticleFields.ToDictionary(
          pair => pair.Key,
          pair => ResolveColumns(headerColumns, pair.Value));

      int idColumn = SingleColumn(headerColumns, ExcelColumns.Id);
      int authorIdColumn = SingleColumn(headerColumns, ExcelColumns.AuthorId);
      int lastNameColumn = SingleColumn(headerColumns, ExcelColumns.LastName);
      int initialsColumn = SingleColumn(headerColumns, ExcelColumns.Initials);
      int emailColumn = SingleColumn(headerColumns, ExcelColumns.Email);
      int keywordColumn = SingleColumn(headerColumns, ExcelColumns.Keyword);

      var articles = new Dictionary<long, Article>();
      foreach (IXLRangeRow row in worksheet.RangeUsed()!.RowsUsed().Skip(1)) {
        string? rawId = CellText(row, idColumn);
        if (!long.TryParse(rawId, NumberStyles.Any, CultureInfo.InvariantCulture, out long id)) {
          continue;
        }

        if (!articles.TryGetValue(id, out Article? article)) {
          article = new Article { Id = id };
          articles[id] = article;
        }

        MergeArticleFields(article, row, fieldColumns);
        MergeAuthor(article, row, authorIdColumn, lastNameColumn, initialsColumn, emailColumn);
        MergeKeyword(article, row, keywordColumn);
      }

      return articles.Values.ToList();
    }

    private static void MergeArticleFields(Article article, IXLRangeRow row,
        Dictionary<string, int[]> fieldColumns) {
      // Bibliographic.
      if (string.IsNullOrEmpty(article.Title)) {
        article.Title = FirstValue(row, fieldColumns["title"]) ?? string.Empty;
      }

      article.YearPubl ??= ParseInt(FirstValue(row, fieldColumns["yearpubl"]));
      article.Publisher ??= FirstValue(row, fieldColumns["publisher"]);
      article.Issn ??= FirstValue(row, fieldColumns["issn"]);
      article.Isbn ??= FirstValue(row, fieldColumns["isbn"]);
      article.Doi ??= FirstValue(row, fieldColumns["doi"]);
      article.Pages ??= FirstValue(row, fieldColumns["pages"]);
      article.Volume ??= FirstValue(row, fieldColumns["volume"]);
      article.Number ??= FirstValue(row, fieldColumns["number"]);

      // Indexing.
      article.Vak ??= FirstValue(row, fieldColumns["vak"]);
      article.Rsci ??= FirstValue(row, fieldColumns["rsci"]);
      article.Wos ??= FirstValue(row, fieldColumns["wos"]);
      article.Scopus ??= FirstValue(row, fieldColumns["scopus"]);
      article.WhiteList ??= FirstValue(row, fieldColumns["white_list"]);
      article.Doaj ??= FirstValue(row, fieldColumns["doaj"]);
      article.CoreRisc ??= FirstValue(row, fieldColumns["corerisc"]);
      article.Cited ??= ParseInt(FirstValue(row, fieldColumns["cited"]));
      article.Citation ??= FirstValue(row, fieldColumns["citation"]);

      // Conference.
      article.ConfName ??= FirstValue(row, fieldColumns["confname"]);
      article.ConfPlace ??= FirstValue(row, fieldColumns["confplace"]);
      article.ConfDateBegin ??= FirstValue(row, fieldColumns["confdatebegin"]);
      article.ConfDateEnd ??= FirstValue(row, fieldColumns["confdateend"]);

      // Additional.
      article.Abstract ??= FirstValue(row, fieldColumns["abstract"]);
      article.Supported ??= FirstValue(row, fieldColumns["supported"]);
      article.Reference ??= FirstValue(row, fieldColumns["reference"]);
      article.Genre ??= FirstValue(row, fieldColumns["genre"]);
      article.Type ??= FirstValue(row, fieldColumns["type"]);
    }

    private static void MergeAuthor(Article article, IXLRangeRow row, int authorIdColumn,
        int lastNameColumn, int initialsColumn, int emailColumn) {
      string? rawAuthorId = CellText(row, authorIdColumn);
      if (!long.TryParse(rawAuthorId, NumberStyles.Any, CultureInfo.InvariantCulture,
              out long authorId)) {
        return;
      }

      if (article.Authors.Any(existing => existing.AuthorId == authorId)) {
        return;
      }

      article.Authors.Add(new Author {
        AuthorId = authorId,
        LastName = CellText(row, lastNameColumn) ?? string.Empty,
        Initials = CellText(row, initialsColumn) ?? string.Empty,
        Email = CellText(row, emailColumn) ?? string.Empty,
      });
    }

    private static void MergeKeyword(Article article, IXLRangeRow row, int keywordColumn) {
      string? keyword = CellText(row, keywordColumn);
      if (!string.IsNullOrWhiteSpace(keyword) && !article.Keywords.Contains(keyword)) {
        article.Keywords.Add(keyword);
      }
    }

    private static Dictionary<string, int> MapHeaders(IXLRangeRow headerRow) {
      var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
      foreach (IXLCell cell in headerRow.Cells()) {
        string name = cell.GetString().Trim();
        if (name.Length > 0 && !headers.ContainsKey(name)) {
          headers[name] = cell.Address.ColumnNumber;
        }
      }

      return headers;
    }

    private static int[] ResolveColumns(Dictionary<string, int> headers, string[] candidates) {
      return candidates
          .Where(headers.ContainsKey)
          .Select(name => headers[name])
          .ToArray();
    }

    private static int SingleColumn(Dictionary<string, int> headers, string name) {
      return headers.TryGetValue(name, out int column) ? column : -1;
    }

    private static string? FirstValue(IXLRangeRow row, int[] columns) {
      foreach (int column in columns) {
        string? value = CellText(row, column);
        if (!string.IsNullOrWhiteSpace(value)) {
          return value;
        }
      }

      return null;
    }

    private static string? CellText(IXLRangeRow row, int column) {
      if (column < 1) {
        return null;
      }

      IXLCell cell = row.Cell(column);
      if (cell.IsEmpty()) {
        return null;
      }

      string text = cell.GetString().Trim();
      return text.Length == 0 ? null : text;
    }

    private static int? ParseInt(string? value) {
      return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out int result)
          ? result
          : null;
    }
  }
}
