using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ELibraryStatistics.Data;
using ELibraryStatistics.Models;
using ELibraryStatistics.Services;

namespace ELibraryStatistics.ViewModels {
  // Page 3: searches articles by title, publication year and the "corerisc"
  // indexing flag, then shows the full record of the selected article. The
  // selection can be exported to CSV.
  public class ArticleSearchViewModel : ViewModelBase {
    private static readonly string[] CoreRiscChoices = { "Все", "Да", "Нет" };

    private readonly ArticleRepository _articles;
    private readonly ExportService _exportService;

    private string _titleText = string.Empty;
    private string _yearText = string.Empty;
    private string _selectedCoreRisc = CoreRiscChoices[0];
    private Article? _selectedResult;
    private Article? _details;
    private string _statusMessage = string.Empty;

    public ArticleSearchViewModel(ArticleRepository articles, ExportService exportService) {
      _articles = articles;
      _exportService = exportService;

      Results = new ObservableCollection<Article>();
      SearchCommand = new RelayCommand(Search);
      ExportCommand = new AsyncRelayCommand(ExportAsync, () => _details != null);
    }

    public IDialogService? DialogService { get; set; }

    public ObservableCollection<Article> Results { get; }

    public string[] CoreRiscOptions => CoreRiscChoices;

    public IRelayCommand SearchCommand { get; }

    public IAsyncRelayCommand ExportCommand { get; }

    public string Version => AppVersion;

    public string TitleText {
      get => _titleText;
      set => SetProperty(ref _titleText, value);
    }

    public string YearText {
      get => _yearText;
      set => SetProperty(ref _yearText, value);
    }

    public string SelectedCoreRisc {
      get => _selectedCoreRisc;
      set => SetProperty(ref _selectedCoreRisc, value);
    }

    public string StatusMessage {
      get => _statusMessage;
      private set => SetProperty(ref _statusMessage, value);
    }

    public Article? SelectedResult {
      get => _selectedResult;
      set {
        if (SetProperty(ref _selectedResult, value)) {
          LoadDetails(value);
        }
      }
    }

    public bool HasSelection => _details != null;

    public string SelectedSummary => _details is null ? string.Empty : Describe(_details);

    private void Search() {
      Results.Clear();
      SelectedResult = null;

      var criteria = new ArticleSearchCriteria {
        TitleContains = string.IsNullOrWhiteSpace(TitleText) ? null : TitleText.Trim(),
        Year = ParseYear(YearText),
        CoreRisc = SelectedCoreRisc switch {
          "Да" => true,
          "Нет" => false,
          _ => null,
        },
      };

      foreach (Article article in _articles.Search(criteria)) {
        Results.Add(article);
      }

      StatusMessage = Results.Count == 0
          ? "Статьи не найдены."
          : $"Найдено статей: {Results.Count}.";
    }

    private void LoadDetails(Article? result) {
      // Reload the article so its authors and keywords are populated.
      _details = result is null ? null : _articles.GetById(result.Id);
      OnPropertyChanged(nameof(HasSelection));
      OnPropertyChanged(nameof(SelectedSummary));
      ExportCommand.NotifyCanExecuteChanged();
    }

    private async Task ExportAsync() {
      if (DialogService is null || _details is null) {
        return;
      }

      string? path = await DialogService.PickSaveFileAsync($"article_{_details.Id}", "csv");
      if (string.IsNullOrEmpty(path)) {
        return;
      }

      try {
        Article article = _details;
        await Task.Run(() => _exportService.ExportArticleToCsv(article, path));
        StatusMessage = "Данные статьи сохранены в CSV.";
      } catch (Exception ex) {
        StatusMessage = "Ошибка экспорта: " + ex.Message;
      }
    }

    private static int? ParseYear(string text) {
      return int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out int year)
          ? year
          : null;
    }

    private static string Describe(Article article) {
      var builder = new StringBuilder();
      builder.AppendLine($"Название: {article.Title}");
      builder.AppendLine($"Id: {article.Id}");
      Append(builder, "Год", article.YearPubl?.ToString(CultureInfo.InvariantCulture));
      Append(builder, "Издатель", article.Publisher);
      Append(builder, "ISSN", article.Issn);
      Append(builder, "ISBN", article.Isbn);
      Append(builder, "DOI", article.Doi);
      Append(builder, "Том", article.Volume);
      Append(builder, "Номер", article.Number);
      Append(builder, "Страницы", article.Pages);
      Append(builder, "Жанр", article.Genre);
      Append(builder, "Тип", article.Type);
      Append(builder, "ВАК", article.Vak);
      Append(builder, "РИНЦ", article.Rsci);
      Append(builder, "Ядро РИНЦ (corerisc)", article.CoreRisc);
      Append(builder, "WoS", article.Wos);
      Append(builder, "Scopus", article.Scopus);
      Append(builder, "Конференция", article.ConfName);
      Append(builder, "Цитирований", article.Cited?.ToString(CultureInfo.InvariantCulture));

      if (article.Authors.Count > 0) {
        builder.Append("Авторы: ");
        for (int i = 0; i < article.Authors.Count; i++) {
          Author author = article.Authors[i];
          builder.Append($"{author.LastName} {author.Initials}");
          builder.Append(i < article.Authors.Count - 1 ? "; " : "\n");
        }
      }

      if (article.Keywords.Count > 0) {
        builder.AppendLine("Ключевые слова: " + string.Join(", ", article.Keywords));
      }

      Append(builder, "Источник", article.SourceFile);
      return builder.ToString().TrimEnd();
    }

    private static void Append(StringBuilder builder, string label, string? value) {
      if (!string.IsNullOrWhiteSpace(value)) {
        builder.AppendLine($"{label}: {value}");
      }
    }
  }
}
