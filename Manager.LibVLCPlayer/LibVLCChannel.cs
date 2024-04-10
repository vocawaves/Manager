using LibVLCSharp.Shared;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.Audio;
using Manager.Shared.Events.General;
using Manager.Shared.Events.Video;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.Audio;
using Manager.Shared.Interfaces.General;
using Manager.Shared.Interfaces.Video;

namespace Manager.LibVLCPlayer;

public class LibVLCChannel : IMediaChannel, IVideoChannel, IChannelSupportsAudioSlaves
{
    private readonly Media _media;
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
    public MediaItem MediaItem { get; }
    public List<PositionTrigger> PositionTriggers { get; } = new();
    private readonly List<PositionTrigger> _positionTriggersAlreadyTriggered = new();

    public LibVLC LibVLCInstance { get; private set; }
    public MediaPlayer MediaPlayer { get; private set; }

    public IExternalPlayerSurface? ExternalVideoSurface { get; private set; }

    public LibVLCChannel(IBackendService backend, MediaItem item, LibVLC libVLCInstance, MediaPlayer mediaPlayer,
        Media media)
    {
        this._media = media;
        this.AssociatedBackend = backend;
        this.MediaItem = item;
        this.LibVLCInstance = libVLCInstance;
        this.MediaPlayer = mediaPlayer;
        this.SetupEvents();
    }
    
    private void SetupEvents()
    {
        this.MediaPlayer.EndReached += (sender, args) =>
        {
            this.MediaPlayer.Media = this._media; // Reset media
            this.Ended?.InvokeAndForget(this, EventArgs.Empty);
        };
        this.MediaPlayer.PositionChanged += (sender, args) =>
        {
            var pos = TimeSpan.FromMilliseconds(this.MediaPlayer.Time);
            this.PositionChanged?.InvokeAndForget(this, new ChannelPositionChangedEventArgs(pos));
        };
        this.MediaPlayer.PositionChanged += (sender, args) =>
        {
            var pos = TimeSpan.FromMilliseconds(this.MediaPlayer.Time);
            foreach (var trigger in this.PositionTriggers)
            {
                if (this._positionTriggersAlreadyTriggered.Contains(trigger))
                    continue;
                if (pos < trigger.Position) 
                    continue;
                this.PositionTriggerInvoked?.InvokeAndForget(this, new ChannelPositionTriggerInvokedEventArgs(trigger));
                this._positionTriggersAlreadyTriggered.Add(trigger);
            }
        };
    }

    public async ValueTask<bool> PlayAsync()
    {
        var playTcs = new TaskCompletionSource<bool>();
        void OnPlaying(object? sender, EventArgs e)
        {
            playTcs?.SetResult(true);
            this.MediaPlayer.Playing -= OnPlaying;
        }

        try
        {
            this.MediaPlayer.Position = 0;
            this.MediaPlayer.Playing += OnPlaying;
            this.MediaPlayer.Play();
            await playTcs.Task;
            this.Playing?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Playing));
        }
        catch (Exception e)
        {
            this.MediaPlayer.Playing -= OnPlaying; // Ensure we don't leak the event handler
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
            this.MediaPlayer.Paused -= OnPaused;
        }
        
        try
        {
            this.MediaPlayer.Paused += OnPaused;
            this.MediaPlayer.Pause();
            await pauseTcs.Task;
            this.Paused?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Paused));
        }
        catch (Exception e)
        {
            this.MediaPlayer.Paused -= OnPaused; // Ensure we don't leak the event handler
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
            this.MediaPlayer.Playing -= OnResumed;
        }
        
        try
        {
            this.MediaPlayer.Playing += OnResumed;
            this.MediaPlayer.Play();
            await resumeTcs.Task;
            this.Resumed?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Playing));
        }
        catch (Exception e)
        {
            this.MediaPlayer.Playing -= OnResumed; // Ensure we don't leak the event handler
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
            this.MediaPlayer.Stopped -= OnStopped;
        }
        
        try
        {
            this.MediaPlayer.Stopped += OnStopped;
            this.MediaPlayer.Stop();
            await stopTcs.Task;
            this.MediaPlayer.Media = this._media; // Reset media
            this.Stopped?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Stopped));
        }
        catch (Exception e)
        {
            this.MediaPlayer.Stopped -= OnStopped; // Ensure we don't leak the event handler
            return false;
        }
        
        return true;
    }

    public ValueTask<ChannelState> GetStateAsync()
    {
        var state = this.MediaPlayer.State switch
        {
            VLCState.NothingSpecial => ChannelState.Stopped,
            VLCState.Playing => ChannelState.Playing,
            VLCState.Paused => ChannelState.Paused,
            VLCState.Stopped => ChannelState.Stopped,
            VLCState.Ended => ChannelState.Ended,
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
        var pos = this.MediaPlayer.Time;
        return ValueTask.FromResult<TimeSpan?>(TimeSpan.FromMilliseconds(pos));
    }

    public ValueTask<bool> SetPositionAsync(double positionMs)
    {
        this.MediaPlayer.Time = (long)positionMs;
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> SetPositionAsync(TimeSpan position)
    {
        this.MediaPlayer.Time = (long)position.TotalMilliseconds;
        return ValueTask.FromResult(true);
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
        var len = this.MediaPlayer.Length;
        return ValueTask.FromResult<TimeSpan?>(TimeSpan.FromMilliseconds(len));
    }

    public ValueTask<bool> DestroyAsync()
    {
        this.MediaPlayer.Dispose();
        this.LibVLCInstance.Dispose();
        return ValueTask.FromResult(true);
    }

    public ValueTask<TView> CreateExternalVideoSurfaceAsync<TView>(string? name = null)
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
        this.DestroyAsync();
        return ValueTask.CompletedTask;
    }

    public event AsyncEventHandler<ChannelAudioSlaveAddedEventArgs>? AudioSlaveAdded;
    public event AsyncEventHandler<ChannelAudioSlaveRemovedEventArgs>? AudioSlaveRemoved;
    public List<AudioItem> AudioSlaves { get; } = new();
    public async ValueTask<bool> AddAudioSlaveAsync(AudioItem audioItem)
    {
        var path = await audioItem.GetCachePathAsync();
        if (string.IsNullOrWhiteSpace(path))
            return false;
        var uri = new Uri(path);
        this.MediaPlayer.AddSlave(MediaSlaveType.Audio, uri.AbsoluteUri, true);
        this.AudioSlaves.Add(audioItem);
        this.AudioSlaveAdded?.InvokeAndForget(this, new ChannelAudioSlaveAddedEventArgs(audioItem));
        return true;
    }

    public ValueTask<bool> RemoveAudioSlaveAsync(AudioItem audioItem)
    {
        throw new NotImplementedException();
    }
}