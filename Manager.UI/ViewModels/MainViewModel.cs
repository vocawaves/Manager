using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using Manager.Shared.Interfaces.Data;
using Manager.UI.ViewModels.Data.Components;
using Microsoft.Extensions.Logging;

namespace Manager.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{    
    public ObservableCollection<CacheItemViewModel> CacheItems { get; } = new();
    
    private IStreamingServiceSource _streamingServiceSource;
    private IFileSystemSource _fileSystemSource;
    
    public MainViewModel()
    {
        var lf = new LoggerFactory();
        _fileSystemSource = new LocalDataService.LocalDataService(lf, "Local", 0);
        _streamingServiceSource = new YouTubeDataService.YouTubeDataService(lf, "YouTube", 0);
        _ = Task.Run(DemoInit);
    }
    
    private async Task DemoInit()
    {
        await _fileSystemSource.InitializeAsync();
        await _streamingServiceSource.InitializeAsync();
        
        var a = DemoLocals();
        var b = DemoYouTube();
        await Task.WhenAll(a, b);
    }
    
    private async Task DemoLocals()
    {
        var fileAsAudioService = _fileSystemSource as IAudioDataSource;
        if (fileAsAudioService == null) 
            return;

        var localsToGet = new[]
        {
            "C:\\Users\\Sekoree\\Music\\Sghenny Madattak - Na Na Na (Frenchcore).flac",
            "C:\\Users\\Sekoree\\Music\\CircusP - 01. Metronome.flac",
            "C:\\Users\\Sekoree\\Music\\iTunes\\iTunes Media\\Music\\MAD MEDiCiNE\\Mad MEDiCATiON\\05 Minzai.m4a"
        };
        
        foreach (var local in localsToGet)
        {
            var localItem = await fileAsAudioService.GetAudioItemAsync(local);
            if (localItem == null) 
                continue;
            
            var localItemViewModel = new CacheItemViewModel(localItem);
            await Dispatcher.UIThread.InvokeAsync(() => CacheItems.Add(localItemViewModel));
        }
    }
    
    private async Task DemoYouTube()
    {
        var youtubeAsAudioService = _streamingServiceSource as IAudioDataSource;
        if (youtubeAsAudioService == null) 
            return;
        
        var youtubeToGet = new[] 
        {
            "https://www.youtube.com/watch?v=zxKVW3mNf_8",
            "https://www.youtube.com/watch?v=LaEgpNBt-bQ",
            "https://www.youtube.com/watch?v=jPPiy_NKRzU"
        };
        
        foreach (var youtube in youtubeToGet)
        {
            var youtubeItem = await youtubeAsAudioService.GetAudioItemAsync(youtube);
            if (youtubeItem == null) 
                continue;
            
            var youtubeItemViewModel = new CacheItemViewModel(youtubeItem);
            await Dispatcher.UIThread.InvokeAsync(() => CacheItems.Add(youtubeItemViewModel));
        }
    }
}
