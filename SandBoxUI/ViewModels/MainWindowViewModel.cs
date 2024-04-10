using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Threading;
using Manager.BassPlayer;
using Manager.LibMPVPlayer;
using Manager.LibMPVPlayer.Avalonia;
using Manager.LibVLCPlayer;
using Manager.LibVLCPlayer.Avalonia;
using Manager.Shared.Interfaces.General;
using Manager.Shared.Interfaces.Video;
using Manager.YouTubeDataService;
using ReactiveUI;
using ValueTaskSupplement;

namespace SandBoxUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private IExternalPlayerSurface? _playerSurface;

    public IExternalPlayerSurface? PlayerSurface
    {
        get => this._playerSurface;
        set
        {
            if (value != null && this._playerSurface == value)
                return;
            this._playerSurface = value;
            this.RaisePropertyChanged();
        }
    }

    public async Task TestDemo()
    {
        try
        {
            var yt = new YouTubeDataService("Test", 0);
            await yt.InitializeAsync();
            var vlc = new LibVLCBackend("Test", 0);
            await vlc.InitializeAsync();
            var mpv = new LibMPVBackend("Test", 0);
            await mpv.InitializeAsync();
            var bass = new BassBackend("Test", 0);
            await bass.InitializeAsync();

            var video = await yt.GetVideoItemAsync("https://www.youtube.com/watch?v=DRZjKAHoP34");
            var audio = await yt.GetAudioItemAsync("https://www.youtube.com/watch?v=DRZjKAHoP34");
            if (video == null || audio == null)
                return;

            video.CacheProgressChanged += (sender, args) =>
            {
                Debug.WriteLine($"Cache progress: {args.Progress}");
                return ValueTask.CompletedTask;
            };
            var couldCache = await video.CacheAsync();
            var couldCacheAudio = await audio.CacheAsync();
            if (!couldCache || !couldCacheAudio)
                return;

            var chan = await mpv.CreateChannelAsync(video);
            var audioChan = await bass.CreateChannelAsync(audio);
            if (chan == null)
                return;

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                this.PlayerSurface = await chan.CreateExternalVideoSurfaceAsync<MPVVideoControl>("Test").ConfigureAwait(true);
            });

            var asMediaChannel = chan as LibMPVChannel;
            if (asMediaChannel == null || audioChan == null)
                return;

            await asMediaChannel.PlayAsync().ConfigureAwait(true);
            await audioChan.PlayAsync().ConfigureAwait(true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}