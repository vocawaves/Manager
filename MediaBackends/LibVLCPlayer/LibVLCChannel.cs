using LibVLCSharp.Shared;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.Audio;
using Manager.Shared.Events.General;
using Manager.Shared.Events.Video;
using Manager.Shared.Extensions;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.Audio;
using Manager.Shared.Interfaces.General;
using Manager.Shared.Interfaces.Video;
using Microsoft.Extensions.Logging;

namespace Manager.MediaBackends.LibVLCPlayer;

public class LibVLCChannel : IMediaChannel, IVideoChannel, IChannelSupportsAudioSlaves
{
    private readonly ILogger<LibVLCChannel>? _logger;

    public event AsyncEventHandler? Playing;
    public event AsyncEventHandler? Paused;
    public event AsyncEventHandler? Resumed;
    public event AsyncEventHandler? Stopped;
    public event AsyncEventHandler? Ended;
    public event AsyncEventHandler<ChannelStateChangedEventArgs>? StateChanged;
    public event AsyncEventHandler<ChannelPositionChangedEventArgs>? PositionChanged;
    public event AsyncEventHandler<ChannelPositionTriggerInvokedEventArgs>? PositionTriggerInvoked;
    public event AsyncEventHandler<ChannelAudioSlaveAddedEventArgs>? AudioSlaveAdded;
    public event AsyncEventHandler<ChannelAudioSlaveRemovedEventArgs>? AudioSlaveRemoved;
    public event AsyncEventHandler<ExternalPlayerSurfaceChangedEventArgs>? ExternalVideoSurfaceChanged;

    public IBackendService AssociatedBackend { get; }
    public MediaPlayer Player { get; }
    public MediaItem MediaItem { get; }
    
    public TimeSpan? Position { get; private set; }
    public TimeSpan? Length { get; private set; }
    
    public List<PositionTrigger> PositionTriggers { get; } = new();
    private readonly List<PositionTrigger> _positionTriggersAlreadyTriggered = new();
    
    public List<MediaItem> AudioSlaves { get; } = new();
    public IExternalPlayerSurface? ExternalVideoSurface { get; private set; }

    public LibVLCChannel(IBackendService backend, MediaItem mediaItem, MediaPlayer player,
        ILogger<LibVLCChannel>? logger = null)
    {
        this._logger = logger;
        this.AssociatedBackend = backend;
        this.MediaItem = mediaItem;
        this.Player = player;
        if (player.Media == null)
            this._logger?.LogError("Media on player is null");
        else
            this.Length = TimeSpan.FromMilliseconds(player.Media.Duration);
        this.Position = TimeSpan.FromMilliseconds(player.Time);
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        this.Player.EndReached += (sender, args) =>
        {
            this.Ended?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Ended));
            this._logger?.LogDebug("Media playback ended");
        };
        this.Player.TimeChanged += (sender, args) =>
        {
            this.Position = TimeSpan.FromMilliseconds(args.Time);
            this.PositionChanged?.InvokeAndForget(this, new ChannelPositionChangedEventArgs(this.Position.Value));
        };
    }

    //public ValueTask<bool> PrepareForPlayAsync(IExternalPlayerSurface surface)
    //{
    //    throw new NotImplementedException();
    //}

    public async ValueTask<bool> PlayAsync()
    {
        var playTcs = new TaskCompletionSource<bool>();
        void OnPlaying(object? sender, EventArgs e)
        {
            playTcs.SetResult(true);
            this.Player.Playing -= OnPlaying;
        }

        try
        {
            this.Player.Playing += OnPlaying;
            this.Player.Play();
            await playTcs.Task.ConfigureAwait(false);
            this.Playing?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Playing));
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to play media");
            this.Player.Playing -= OnPlaying; // Ensure we don't leak the event handler
            return false;
        }

        return true;
    }

    public async ValueTask<bool> PauseAsync()
    {
        var pauseTcs = new TaskCompletionSource<bool>();

        void OnPaused(object? sender, EventArgs e)
        {
            pauseTcs?.SetResult(true);
            this.Player.Paused -= OnPaused;
        }

        try
        {
            await await Task.Factory.StartNew(async () =>
            {
                this.Player.Paused += OnPaused;
                this.Player.Pause();
                await pauseTcs.Task;
            });
            this.Paused?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Paused));
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to pause media");
            this.Player.Paused -= OnPaused; // Ensure we don't leak the event handler
            return false;
        }

        return true;
    }

    public async ValueTask<bool> ResumeAsync()
    {
        var resumeTcs = new TaskCompletionSource<bool>();

        void OnResumed(object? sender, EventArgs e)
        {
            resumeTcs?.SetResult(true);
            this.Player.Playing -= OnResumed;
        }

        try
        {
            await await Task.Factory.StartNew(async () =>
            {
                this.Player.Playing += OnResumed;
                this.Player.Play();
                await resumeTcs.Task;
            });
            this.Resumed?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Playing));
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to resume media");
            this.Player.Playing -= OnResumed; // Ensure we don't leak the event handler
            return false;
        }

        return true;
    }

    public async ValueTask<bool> StopAsync()
    {
        var stopTcs = new TaskCompletionSource<bool>();

        void OnStopped(object? sender, EventArgs e)
        {
            stopTcs?.SetResult(true);
            this.Player.Stopped -= OnStopped;
        }

        try
        {
            await await Task.Factory.StartNew(async () =>
            {
                this.Player.Stopped += OnStopped;
                this.Player.Stop();
                await stopTcs.Task;
            });
            //TODO: check if IsPlayReady needs to be reset here
            this.Stopped?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Stopped));
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to stop media");
            return false;
        }

        return true;
    }

    public ValueTask<ChannelState> GetStateAsync()
    {
        var state = this.Player.State switch
        {
            VLCState.NothingSpecial => ChannelState.Stopped,
            VLCState.Playing => ChannelState.Playing,
            VLCState.Paused => ChannelState.Paused,
            VLCState.Stopped => ChannelState.Stopped,
            _ => ChannelState.Invalid
        };
        return ValueTask.FromResult(state);
    }

    public ValueTask<bool> SetStateAsync(ChannelState state)
    {
        switch (state)
        {
            case ChannelState.Playing:
                return this.PlayAsync();
            case ChannelState.Paused:
                return this.PauseAsync();
            case ChannelState.Stopped:
                return this.StopAsync();
            default:
                return new ValueTask<bool>(false);
        }
    }

    public ValueTask<TimeSpan?> GetPositionAsync()
    {
        var pos = this.Player.Time;
        return ValueTask.FromResult<TimeSpan?>(TimeSpan.FromMilliseconds(pos));
    }

    public async ValueTask<bool> SetPositionAsync(double positionMs)
    {
        //TODO: Not sure if TimeChanged will have the right time for this check, maybe also allow if it's within a certain range?
        var posChanged = new TaskCompletionSource<bool>();

        void OnPositionChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
        {
            if (e.Time != (long)positionMs)
                return;
            posChanged.SetResult(true);
            this.Player.TimeChanged -= OnPositionChanged;
        }

        this.Player.TimeChanged += OnPositionChanged;
        await await Task.Factory.StartNew(async () =>
        {
            this.Player.Time = (long)positionMs;
            await posChanged.Task;
        });
        return true;
    }

    public ValueTask<bool> SetPositionAsync(TimeSpan position)
    {
        return SetPositionAsync(position.TotalMilliseconds);
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
        var len = this.Player.Media!.Duration;
        return ValueTask.FromResult<TimeSpan?>(TimeSpan.FromMilliseconds(len));
    }

    public ValueTask<bool> SetExternalVideoSurfaceAsync(IExternalPlayerSurface surface)
    {
        if (this.ExternalVideoSurface == surface)
        {
            return ValueTask.FromResult(true);
        }

        if (surface is not IVLCVideoControl vlcControl)
            return ValueTask.FromResult(false);

        
        this.ExternalVideoSurface = vlcControl;
        this.ExternalVideoSurfaceChanged?.InvokeAndForget(this, new ExternalPlayerSurfaceChangedEventArgs(surface));
        return ValueTask.FromResult(true);
    }


    public ValueTask<bool> DestroyAsync()
    {
        this.Player.Media?.Dispose();
        this.Player.Dispose();
        return ValueTask.FromResult(true);
    }

    public async ValueTask DisposeAsync()
    {
        await this.DestroyAsync();
    }

    public async ValueTask<bool> AddAudioSlaveAsync(MediaItem audioItem)
    {
        var path = await audioItem.GetCachedPathAsync();
        if (string.IsNullOrWhiteSpace(path))
            return false;
        var uri = new Uri(path);
        this.Player.AddSlave(MediaSlaveType.Audio, uri.AbsoluteUri, true);
        this.AudioSlaves.Add(audioItem);
        this.AudioSlaveAdded?.InvokeAndForget(this, new ChannelAudioSlaveAddedEventArgs(audioItem));
        return true;
    }

    public async ValueTask<bool> RemoveAudioSlaveAsync(MediaItem audioItem)
    {
        var path = await audioItem.GetCachedPathAsync();
        if (string.IsNullOrWhiteSpace(path))
            return false;
        var uri = new Uri(path);
        //TODO: Find a way to remove the slave
        this.AudioSlaves.Remove(audioItem);
        this.AudioSlaveRemoved?.InvokeAndForget(this, new ChannelAudioSlaveRemovedEventArgs(audioItem));
        return true;
    }
}