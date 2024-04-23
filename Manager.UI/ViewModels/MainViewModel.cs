using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using Manager.Shared.Interfaces.Audio;
using Manager.Shared.Interfaces.Data;
using Manager.UI.ViewModels.Data.Components;

namespace Manager.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{    
    public ObservableCollection<BaseCacheItemViewModel> CacheItems { get; } = new();
    
    private IStreamingServiceSource _streamingServiceSource;
    private IFileSystemSource _fileSystemSource;
    
    public MainViewModel()
    {
        _fileSystemSource = new LocalDataService.LocalDataService( "Local", 0);
        _streamingServiceSource = new YouTubeDataService.YouTubeDataService( "YouTube", 0);
        _ = Task.Run(DemoInit);
    }
    
    private async Task DemoInit()
    {
        await GlobalAudioBackendService.InitializeAsync();
        
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
            
            var localItemViewModel = new AudioCacheItemViewModel(localItem);
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
            "https://www.youtube.com/watch?v=N13AJER09K8",
            "https://www.youtube.com/watch?v=cVfxLn8Kw84",
            "https://www.youtube.com/watch?v=tpodtZo5HRg",
            "https://www.youtube.com/watch?v=zxKVW3mNf_8",
            "https://www.youtube.com/watch?v=LaEgpNBt-bQ",
            "https://www.youtube.com/watch?v=jPPiy_NKRzU",
            "https://www.youtube.com/watch?v=hE2jLYw7GgM"
        };
        
        foreach (var youtube in youtubeToGet)
        {
            var youtubeItem = await youtubeAsAudioService.GetAudioItemAsync(youtube);
            if (youtubeItem == null) 
                continue;
            
            var youtubeItemViewModel = new AudioCacheItemViewModel(youtubeItem);
            await Dispatcher.UIThread.InvokeAsync(() => CacheItems.Add(youtubeItemViewModel));
        }
    }

    #region Demo

    public static IAudioBackendService GlobalAudioBackendService { get; } = new BassPlayer.BassBackend("Bass", 0);

    #endregion
}
