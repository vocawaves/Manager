using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Manager.UI.Models.Data;

namespace Manager.UI.ViewModels.Data;

public partial class LocalDataViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<FolderModel> _folders = new();
}