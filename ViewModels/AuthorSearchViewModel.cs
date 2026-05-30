using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ELibraryStatistics.Data;
using ELibraryStatistics.Models;
using ELibraryStatistics.Services;

namespace ELibraryStatistics.ViewModels {
  // Page 2: searches authors by surname and, once one is selected, shows every
  // article linked to that AuthorId. Authors are matched by AuthorId because
  // their initials may differ between articles. The selection can be exported
  // to CSV.
  public class AuthorSearchViewModel : ViewModelBase {
    private readonly ArticleRepository _articles;
    private readonly AuthorRepository _authors;
    private readonly ExportService _exportService;

    private string _searchText = string.Empty;
    private Author? _selectedAuthor;
    private AuthorDetails? _details;
    private string _statusMessage = string.Empty;

    public AuthorSearchViewModel(ArticleRepository articles, AuthorRepository authors,
        ExportService exportService) {
      _articles = articles;
      _authors = authors;
      _exportService = exportService;

      Results = new ObservableCollection<Author>();
      Articles = new ObservableCollection<Article>();
      SearchCommand = new RelayCommand(Search);
      ExportCommand = new AsyncRelayCommand(ExportAsync, () => _details != null);
    }

    public IDialogService? DialogService { get; set; }

    public ObservableCollection<Author> Results { get; }

    public ObservableCollection<Article> Articles { get; }

    public IRelayCommand SearchCommand { get; }

    public IAsyncRelayCommand ExportCommand { get; }

    public string Version => AppVersion;

    public string SearchText {
      get => _searchText;
      set => SetProperty(ref _searchText, value);
    }

    public string StatusMessage {
      get => _statusMessage;
      private set => SetProperty(ref _statusMessage, value);
    }

    public Author? SelectedAuthor {
      get => _selectedAuthor;
      set {
        if (SetProperty(ref _selectedAuthor, value)) {
          LoadDetails(value);
        }
      }
    }

    public bool HasSelection => _details != null;

    public string SelectedSummary {
      get {
        if (_details is null) {
          return string.Empty;
        }

        Author author = _details.Author;
        return $"{author.LastName} {author.Initials}  (AuthorId: {author.AuthorId})\n" +
            $"Email: {author.Email}\nСтатей: {_details.Articles.Count}";
      }
    }

    private void Search() {
      Results.Clear();
      SelectedAuthor = null;
      if (string.IsNullOrWhiteSpace(SearchText)) {
        StatusMessage = "Введите фамилию автора.";
        return;
      }

      foreach (Author author in _authors.SearchByLastName(SearchText.Trim())) {
        Results.Add(author);
      }

      StatusMessage = Results.Count == 0
          ? "Авторы не найдены."
          : $"Найдено авторов: {Results.Count}.";
    }

    private void LoadDetails(Author? author) {
      Articles.Clear();
      if (author is null) {
        _details = null;
      } else {
        _details = new AuthorDetails { Author = author };
        foreach (Article article in _articles.GetByAuthorId(author.AuthorId)) {
          _details.Articles.Add(article);
          Articles.Add(article);
        }
      }

      OnPropertyChanged(nameof(HasSelection));
      OnPropertyChanged(nameof(SelectedSummary));
      ExportCommand.NotifyCanExecuteChanged();
    }

    private async Task ExportAsync() {
      if (DialogService is null || _details is null) {
        return;
      }

      string suggested = $"author_{_details.Author.AuthorId}";
      string? path = await DialogService.PickSaveFileAsync(suggested, "csv");
      if (string.IsNullOrEmpty(path)) {
        return;
      }

      try {
        AuthorDetails details = _details;
        await Task.Run(() => _exportService.ExportAuthorToCsv(details, path));
        StatusMessage = "Данные автора сохранены в CSV.";
      } catch (Exception ex) {
        StatusMessage = "Ошибка экспорта: " + ex.Message;
      }
    }
  }
}
