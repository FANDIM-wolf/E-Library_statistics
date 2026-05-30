using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ELibraryStatistics.Data;
using ELibraryStatistics.Models;
using ELibraryStatistics.Services;

namespace ELibraryStatistics.ViewModels {
  // Page 1: loads an export file, shows the aggregate statistics and the file
  // upload history, allows saving the statistics report and deleting a file's
  // data from the database.
  public class LoadStatisticsViewModel : ViewModelBase {
    private readonly ArticleRepository _articles;
    private readonly AuthorRepository _authors;
    private readonly SourceFileRepository _sourceFiles;
    private readonly ImportService _importService;
    private readonly ExportService _exportService;

    private int _fileCount;
    private int _authorCount;
    private int _articleCount;
    private string _statusMessage = string.Empty;
    private bool _isBusy;

    public LoadStatisticsViewModel(ArticleRepository articles, AuthorRepository authors,
        SourceFileRepository sourceFiles, ImportService importService,
        ExportService exportService) {
      _articles = articles;
      _authors = authors;
      _sourceFiles = sourceFiles;
      _importService = importService;
      _exportService = exportService;

      History = new ObservableCollection<SourceFileRecord>();
      LoadFileCommand = new AsyncRelayCommand(LoadFileAsync);
      SaveStatisticsCommand = new AsyncRelayCommand(SaveStatisticsAsync);
      DeleteFileCommand = new AsyncRelayCommand<SourceFileRecord>(DeleteFileAsync);

      Refresh();
    }

    public IDialogService? DialogService { get; set; }

    public ObservableCollection<SourceFileRecord> History { get; }

    public IAsyncRelayCommand LoadFileCommand { get; }

    public IAsyncRelayCommand SaveStatisticsCommand { get; }

    public IAsyncRelayCommand<SourceFileRecord> DeleteFileCommand { get; }

    public string Version => AppVersion;

    public int FileCount {
      get => _fileCount;
      private set => SetProperty(ref _fileCount, value);
    }

    public int AuthorCount {
      get => _authorCount;
      private set => SetProperty(ref _authorCount, value);
    }

    public int ArticleCount {
      get => _articleCount;
      private set => SetProperty(ref _articleCount, value);
    }

    public string StatusMessage {
      get => _statusMessage;
      private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy {
      get => _isBusy;
      private set => SetProperty(ref _isBusy, value);
    }

    private async Task LoadFileAsync() {
      if (DialogService is null || IsBusy) {
        return;
      }

      string? path = await DialogService.PickExcelFileAsync();
      if (string.IsNullOrEmpty(path)) {
        return;
      }

      try {
        IsBusy = true;
        StatusMessage = "Чтение файла…";
        ImportSession session = await Task.Run(() => _importService.Prepare(path));

        ImportPreview preview = session.Preview;
        StatusMessage =
            $"Найдено статей: {preview.TotalArticles} (новых: {preview.NewArticles}, " +
            $"дубликатов: {preview.DuplicateArticles}); авторов: {preview.TotalAuthors} " +
            $"(новых: {preview.NewAuthors}). Загрузка…";

        SourceFileRecord record = await Task.Run(() => _importService.Commit(session));
        StatusMessage =
            $"Файл «{record.DisplayName}» загружен: добавлено статей {record.ArticleCount}, " +
            $"авторов {record.AuthorCount}.";
        Refresh();
      } catch (Exception ex) {
        StatusMessage = "Ошибка загрузки: " + ex.Message;
      } finally {
        IsBusy = false;
      }
    }

    private async Task SaveStatisticsAsync() {
      if (DialogService is null || IsBusy) {
        return;
      }

      string? path = await DialogService.PickSaveFileAsync("elibrary_statistics", "docx");
      if (string.IsNullOrEmpty(path)) {
        return;
      }

      try {
        IsBusy = true;
        var statistics = new DatabaseStatistics {
          FileCount = FileCount,
          AuthorCount = AuthorCount,
          ArticleCount = ArticleCount,
        };

        SourceFileRecord[] history = System.Linq.Enumerable.ToArray(History);
        await Task.Run(() =>
            _exportService.ExportStatisticsToDoc(statistics, history, Version, path));
        StatusMessage = "Статистика сохранена в файл.";
      } catch (Exception ex) {
        StatusMessage = "Ошибка сохранения: " + ex.Message;
      } finally {
        IsBusy = false;
      }
    }

    private async Task DeleteFileAsync(SourceFileRecord? record) {
      if (record is null || IsBusy) {
        return;
      }

      try {
        IsBusy = true;
        await Task.Run(() => _importService.DeleteFile(record));
        StatusMessage = $"Файл «{record.DisplayName}» удалён из базы.";
        Refresh();
      } catch (Exception ex) {
        StatusMessage = "Ошибка удаления: " + ex.Message;
      } finally {
        IsBusy = false;
      }
    }

    private void Refresh() {
      FileCount = _sourceFiles.Count();
      AuthorCount = _authors.Count();
      ArticleCount = _articles.Count();

      History.Clear();
      foreach (SourceFileRecord record in _sourceFiles.GetAll()) {
        History.Add(record);
      }
    }
  }
}
