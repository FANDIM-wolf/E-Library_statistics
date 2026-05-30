using System.Threading.Tasks;

namespace ELibraryStatistics.Services {
  // Abstracts the native file pickers so that view models stay free of any
  // Avalonia UI types and remain unit-testable.
  public interface IDialogService {
    // Asks the user to choose an Excel workbook to import. Returns null if the
    // dialog is cancelled.
    Task<string?> PickExcelFileAsync();

    // Asks the user where to save a file with the given suggested name and
    // extension (for example "csv" or "docx"). Returns null if cancelled.
    Task<string?> PickSaveFileAsync(string suggestedName, string extension);
  }
}
