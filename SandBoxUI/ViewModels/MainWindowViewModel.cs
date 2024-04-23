using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LibVLCSharp;
using LibVLCSharp.Shared;
using Manager.BassPlayer;
using Manager.Shared.Interfaces.Audio;
using Manager.YouTubeDataService;
using ValueTaskSupplement;

namespace SandBoxUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly LibVLC _libVlc;
    [ObservableProperty] private MediaPlayer? _mediaPlayer;
    [ObservableProperty] private MediaPlayer? _mediaPlayer2;
    [ObservableProperty] private MediaPlayer? _mediaPlayer3;
    [ObservableProperty] private MediaPlayer? _mediaPlayer4;
    [ObservableProperty] private MediaPlayer? _mediaPlayer5;
    [ObservableProperty] private MediaPlayer? _mediaPlayer6;
    [ObservableProperty] private MediaPlayer? _mediaPlayer7;
    [ObservableProperty] private MediaPlayer? _mediaPlayer8;
    [ObservableProperty] private double? _playerHeight = 0;
    [ObservableProperty] private double? _playerWidth = 0;
    private IAudioChannel? _audioChannel;

    public MainWindowViewModel()
    {
        Core.Initialize();
        this._libVlc = new LibVLC("--file-caching=60000", "--high-priority");
    }

    private bool _firstRun = true;
    private readonly BassBackend _bass = new BassBackend("Test", 0);
    private readonly YouTubeDataService _youTube = new YouTubeDataService("Test", 0);

    public async Task TestDemo()
    {
        try
        {
            PlayerHeight = 0;
            PlayerWidth = 0;
            if (MediaPlayer != null && MediaPlayer2 != null)
            {
                await StopPlayersAsync().ConfigureAwait(true);
            }

            if (_firstRun)
            {
                await _bass.InitializeAsync().ConfigureAwait(true);
                await _youTube.InitializeAsync().ConfigureAwait(true);
                _firstRun = false;
            }

            var video = await _youTube.GetVideoItemAsync("https://www.youtube.com/watch?v=DRZjKAHoP34")
                .ConfigureAwait(true);
            var audio = await _youTube.GetAudioItemAsync("https://www.youtube.com/watch?v=DRZjKAHoP34")
                .ConfigureAwait(true);
            if (video == null || audio == null)
                return;

            video.CacheProgressChanged += (sender, args) =>
            {
                Debug.WriteLine($"Cache progress: {args.Progress}");
                return ValueTask.CompletedTask;
            };
            var couldCache = await video.CacheAsync().ConfigureAwait(true);
            var couldCacheAudio = await audio.CacheAsync().ConfigureAwait(true);
            if (!couldCache || !couldCacheAudio)
                return;

            _audioChannel = await _bass.CreateChannelAsync(audio).ConfigureAwait(true);
            var videoCachePath = await video.GetCachePathAsync().ConfigureAwait(true);
            var audioCachePath = await audio.GetCachePathAsync().ConfigureAwait(true);
            if (string.IsNullOrEmpty(videoCachePath) || string.IsNullOrEmpty(audioCachePath) || _audioChannel == null)
                return;

            var videoMedia = new Media(_libVlc, videoCachePath);
            var audioMedia = new Media(_libVlc, audioCachePath);

            var parse = await Task.Factory.StartNew(async () =>
            {
                await videoMedia.Parse().ConfigureAwait(true);
                await audioMedia.Parse().ConfigureAwait(true);
            });
            await parse.ConfigureAwait(true);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MediaPlayer = new MediaPlayer(videoMedia);
                MediaPlayer2 = new MediaPlayer(videoMedia);
                MediaPlayer3 = new MediaPlayer(videoMedia);
                MediaPlayer4 = new MediaPlayer(videoMedia);
                MediaPlayer5 = new MediaPlayer(videoMedia);
                MediaPlayer6 = new MediaPlayer(videoMedia);
                MediaPlayer7 = new MediaPlayer(videoMedia);
                MediaPlayer8 = new MediaPlayer(videoMedia);
            });

            if (MediaPlayer == null || MediaPlayer2 == null || MediaPlayer3 == null || MediaPlayer4 == null ||
                MediaPlayer5 == null || MediaPlayer6 == null || MediaPlayer7 == null || MediaPlayer8 == null)
                return;

            var t = Task.Factory.StartNew(async () =>
            {
                var tcs = await Task.WhenAll(PrimeMediaPlayer(MediaPlayer), PrimeMediaPlayer(MediaPlayer2),
                        PrimeMediaPlayer(MediaPlayer3), PrimeMediaPlayer(MediaPlayer4), PrimeMediaPlayer(MediaPlayer5),
                        PrimeMediaPlayer(MediaPlayer6), PrimeMediaPlayer(MediaPlayer7), PrimeMediaPlayer(MediaPlayer8))
                    .ConfigureAwait(true);

                var tcTasks = tcs.Select(x => x.Task).ToArray();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    PlayerHeight = default;
                    PlayerWidth = default;
                });

                await Task.Delay(250).ConfigureAwait(true);

                MediaPlayer.Play();
                MediaPlayer2.Play();
                MediaPlayer3.Play();
                MediaPlayer4.Play();
                MediaPlayer5.Play();
                MediaPlayer6.Play();
                MediaPlayer7.Play();
                MediaPlayer8.Play();
                await Task.WhenAll(tcTasks).ConfigureAwait(true);
                await _audioChannel.PlayAsync().ConfigureAwait(true);
            });
            await t.ConfigureAwait(true);
            //MediaPlayer.AddSlave(MediaSlaveType.Audio, audioMedia.Mrl, true);

            //var videoMedia2 = new Media(videoCachePath);
            //var audioMedia2 = new Media(audioCachePath);

            //await Task.Factory.StartNew(async () =>
            //{
            //    await videoMedia.Parse().ConfigureAwait(true);
            //    await audioMedia.Parse().ConfigureAwait(true);
            //});

            //await _audioChannel.PlayAsync();
            //await _audioChannel.PauseAsync();

            //await PrimeMediaPlayer(MediaPlayer).ConfigureAwait(true);
            //await PrimeMediaPlayer(MediaPlayer2).ConfigureAwait(true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task<TaskCompletionSource<bool>> PrimeMediaPlayer(MediaPlayer player)
    {
        var voutTcs = new TaskCompletionSource<bool>();

        void OnVoutCreated(object? sender, EventArgs e)
        {
            player.Vout -= OnVoutCreated;
            voutTcs.SetResult(true);
        }

        player.Vout += OnVoutCreated;
        var playTask = await Task.Factory.StartNew(async () =>
        {
            player.Play();
            await voutTcs.Task.ConfigureAwait(true);
        });
        await playTask.ConfigureAwait(true);
        var pauseTcs = new TaskCompletionSource<bool>();

        void OnPaused(object? sender, EventArgs e)
        {
            player.Paused -= OnPaused;
            pauseTcs.SetResult(true);
        }

        player.Paused += OnPaused;
        var pauseTask = await Task.Factory.StartNew(async () =>
        {
            player.Pause();
            await pauseTcs.Task.ConfigureAwait(true);
        });
        await pauseTask.ConfigureAwait(true);
        var seekTask = await Task.Factory.StartNew(() =>
        {
            player.Time = 0;
            return Task.CompletedTask;
        });
        await seekTask.ConfigureAwait(true);
        var playTcs = new TaskCompletionSource<bool>();

        void OnPlaying(object? sender, EventArgs e)
        {
            player.Playing -= OnPlaying;
            playTcs.SetResult(true);
        }

        player.Playing += OnPlaying;
        return playTcs;
    }

    private async Task StopPlayersAsync()
    {
        if (_audioChannel != null)
        {
            await _audioChannel.StopAsync().ConfigureAwait(true);
            await _audioChannel.DisposeAsync().ConfigureAwait(true);
            _audioChannel = null;
        }

        MediaPlayer?.Dispose();
        MediaPlayer2?.Dispose();
        MediaPlayer3?.Dispose();
        MediaPlayer4?.Dispose();
        MediaPlayer5?.Dispose();
        MediaPlayer6?.Dispose();
        MediaPlayer7?.Dispose();
        MediaPlayer8?.Dispose();
    }
}