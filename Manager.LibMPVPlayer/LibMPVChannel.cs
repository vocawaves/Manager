using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.General;
using Manager.Shared.Events.Video;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.General;
using Manager.Shared.Interfaces.Video;
using Nickvision.MPVSharp;

namespace Manager.LibMPVPlayer;

public class LibMPVChannel : IMediaChannel, IVideoChannel
{
    private readonly string _cachePath;
    public event AsyncEventHandler? Playing;
    public event AsyncEventHandler? Paused;
    public event AsyncEventHandler? Resumed;
    public event AsyncEventHandler? Stopped;
    public event AsyncEventHandler? Ended;
    public event AsyncEventHandler<ChannelStateChangedEventArgs>? StateChanged;
    public event AsyncEventHandler<ChannelPositionChangedEventArgs>? PositionChanged;
    public event AsyncEventHandler<ChannelPositionTriggerInvokedEventArgs>? PositionTriggerInvoked;
    public event AsyncEventHandler<ExternalPlayerSurfaceChangedEventArgs>? ExternalVideoSurfaceCreated;

    public IBackendService AssociatedBackend { get; }
    public Client MPVClient { get; }
    public MediaItem MediaItem { get; }
    public List<PositionTrigger> PositionTriggers { get; } = new();

    public IExternalPlayerSurface? ExternalVideoSurface { get; private set; }

    public LibMPVChannel(IBackendService backend, Client mpvClient, MediaItem item, string cachePath)
    {
        this._cachePath = cachePath;
        this.AssociatedBackend = backend;
        this.MPVClient = mpvClient;
        this.MediaItem = item;
    }

    public async ValueTask<bool> PlayAsync()
    {
        var playTcs = new TaskCompletionSource<bool>();
        void OnPlaying(object? sender, EventArgs e)
        {
            this.MPVClient.FileStarted -= OnPlaying;
            playTcs.SetResult(true);
        }
        
        this.MPVClient.FileStarted += OnPlaying;
        //seek to 0
        this.MPVClient.SetProperty("pause", "no");
        await playTcs.Task;
        return true;
    }

    public ValueTask<bool> PauseAsync()
    {
        this.MPVClient.Command("pause");
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> ResumeAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> StopAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<ChannelState> GetStateAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetStateAsync(ChannelState state)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TimeSpan?> GetPositionAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetPositionAsync(double positionMs)
    {
        var inSeconds = positionMs / 1000;
        this.MPVClient.SetProperty("time-pos", inSeconds);
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> SetPositionAsync(TimeSpan position)
    {
        throw new NotImplementedException();
    }

    public ValueTask<PositionTrigger?> AddPositionTriggerAsync(string name, TimeSpan position)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> RemovePositionTriggerAsync(PositionTrigger trigger)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TimeSpan?> GetLengthAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> DestroyAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<TView> CreateExternalVideoSurfaceAsync<TView>(string? name)
        where TView : class, IExternalPlayerSurface
    {
        var surfName = name ?? this.MediaItem.PathTitle;
        var view = TView.CreateSurface(this, surfName);
        if (view is null)
            throw new InvalidOperationException("Failed to create external video surface");
        this.ExternalVideoSurface = view;
        this.ExternalVideoSurfaceCreated?.Invoke(this, new ExternalPlayerSurfaceChangedEventArgs(view));
        return new ValueTask<TView>((TView)view);
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}