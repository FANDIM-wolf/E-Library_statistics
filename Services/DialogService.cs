using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace ELibraryStatistics.Services {
  // Avalonia implementation of IDialogService backed by the storage provider of
  // a top-level window.
  public class DialogService : IDialogService {
    private readonly TopLevel _topLevel;

    public DialogService(TopLevel topLevel) {
      _topLevel = topLevel;
    }

    public async Task<string?> PickExcelFileAsync() {
      var options = new FilePickerOpenOptions {
        Title = "Выберите файл выгрузки E-library",
        AllowMultiple = false,
        FileTypeFilter = new[] {
          new FilePickerFileType("Excel") { Patterns = new[] { "*.xlsx", "*.xls" } },
        },
      };

      IReadOnlyList<IStorageFile> files = await _topLevel.StorageProvider.OpenFilePickerAsync(
          options);
      return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickSaveFileAsync(string suggestedName, string extension) {
      var options = new FilePickerSaveOptions {
        Title = "Сохранить файл",
        SuggestedFileName = suggestedName,
        DefaultExtension = extension,
        FileTypeChoices = new[] {
          new FilePickerFileType(extension.ToUpperInvariant()) {
            Patterns = new[] { "*." + extension },
          },
        },
      };

      IStorageFile? file = await _topLevel.StorageProvider.SaveFilePickerAsync(options);
      return file?.TryGetLocalPath();
    }
  }
}
