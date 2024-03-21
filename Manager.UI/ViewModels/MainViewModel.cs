using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Manager.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private UserControl? _activeDataView;
}
