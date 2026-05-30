using System.Collections.ObjectModel;
using ELibraryStatistics.Data;
using ELibraryStatistics.Services;

namespace ELibraryStatistics.ViewModels {
  // The shell view model. It owns the three tab pages described in the GUI
  // mockups and forwards the dialog service to the pages that need it once the
  // main window exists.
  public class MainWindowViewModel : ViewModelBase {
    public MainWindowViewModel(ArticleRepository articles, AuthorRepository authors,
        SourceFileRepository sourceFiles, ImportService importService,
        ExportService exportService) {
      LoadPage = new LoadStatisticsViewModel(articles, authors, sourceFiles, importService,
          exportService);
      AuthorPage = new AuthorSearchViewModel(articles, authors, exportService);
      ArticlePage = new ArticleSearchViewModel(articles, exportService);

      Pages = new ObservableCollection<ViewModelBase> { LoadPage, AuthorPage, ArticlePage };
    }

    public string Title => "Статистика E-library";

    public ObservableCollection<ViewModelBase> Pages { get; }

    public LoadStatisticsViewModel LoadPage { get; }

    public AuthorSearchViewModel AuthorPage { get; }

    public ArticleSearchViewModel ArticlePage { get; }

    // Connects the dialog service to the pages that open file pickers. Called by
    // the application once the top-level window is created.
    public void AttachDialogService(IDialogService dialogService) {
      LoadPage.DialogService = dialogService;
      AuthorPage.DialogService = dialogService;
      ArticlePage.DialogService = dialogService;
    }
  }
}
