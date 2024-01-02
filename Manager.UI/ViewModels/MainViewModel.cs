using System.Collections.ObjectModel;
using Manager.Services.BassAudio;
using Manager.Services.Data;
using Manager.UI.Models;

namespace Manager.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public ObservableCollection<FileDirectoryItem> DirectoryItems { get; set; } = new();
    
    //private LocalDataService? _dirService = new("LDS_Test", 0);
    //private BassAudioBackendService _audioService = new("BASS_Test", 0);
}
