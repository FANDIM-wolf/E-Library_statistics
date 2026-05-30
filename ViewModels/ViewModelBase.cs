using CommunityToolkit.Mvvm.ComponentModel;

namespace ELibraryStatistics.ViewModels {
  // Base class for all view models. Inherits property-change notification from
  // the MVVM toolkit and is the type matched by the ViewLocator.
  public abstract class ViewModelBase : ObservableObject {
    // The application version shown in the footer of every page.
    public const string AppVersion = "1.0";
  }
}
