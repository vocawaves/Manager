using System.Collections.ObjectModel;
using Manager.Services.Data;
using Manager.UI.Models;

namespace Manager.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public ObservableCollection<FileDirectoryItem> DirectoryItems { get; set; } = new();
    
    private LocalDataService? _dirService = new("Test", 0);
}
