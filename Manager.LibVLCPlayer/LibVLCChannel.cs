using LibVLCSharp;
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
    public event AsyncEventHandler? Playing;
    public event AsyncEventHandler? Paused;
    public event AsyncEventHandler? Resumed;
    public event AsyncEventHandler? Stopped;
    public event AsyncEventHandler? Ended;
    public event AsyncEventHandler<ChannelStateChangedEventArgs>? StateChanged;
    public event AsyncEventHandler<ChannelPositionChangedEventArgs>? PositionChanged;
    public event AsyncEventHandler<ChannelPositionTriggerInvokedEventArgs>? PositionTriggerInvoked;

    public event AsyncEventHandler<ExternalPlayerSurfaceChangedEventArgs>? ExternalVideoSurfaceChanged;

    public IBackendService AssociatedBackend { get; }
    public MediaItem MediaItem { get; }
    public List<PositionTrigger> PositionTriggers { get; } = new();
    private readonly List<PositionTrigger> _positionTriggersAlreadyTriggered = new();

    public IExternalPlayerSurface? ExternalVideoSurface { get; private set; }
    public MediaPlayer? CurrentMediaPlayer { get; private set; }
    public Media? CurrentMedia { get; private set; }
    
    public LibVLCChannel(IBackendService backend, MediaItem item)
    {
        this.AssociatedBackend = backend;
        this.MediaItem = item;
    }

    public async ValueTask<bool> PlayAsync()
    {
        if (this.CurrentMediaPlayer == null)
            return false;

        try
        {
            if(this.ExternalVideoSurface == null)
                return false;
            await this.ExternalVideoSurface.PrepareVideoAsync(this);
            await this.CurrentMediaPlayer.PlayAsync();
            this.Playing?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Playing));
        }
        catch (Exception e) 
        {
            return false;
        }

        return true;
    }

    public async ValueTask<bool> PauseAsync()
    {
        if (this.CurrentMediaPlayer == null)
            return false;
        var pauseTcs = new TaskCompletionSource<bool>();
        void OnPaused(object? sender, EventArgs e)
        {
            pauseTcs?.SetResult(true);
            this.CurrentMediaPlayer!.Paused -= OnPaused;
        }
        
        try
        {
            this.CurrentMediaPlayer.Paused += OnPaused;
            this.CurrentMediaPlayer.Pause();
            await pauseTcs.Task;
            this.Paused?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Paused));
        }
        catch (Exception e)
        {
            this.CurrentMediaPlayer.Paused -= OnPaused; // Ensure we don't leak the event handler
            return false;
        }
        
        return true;
    }

    public async ValueTask<bool> ResumeAsync()
    {
        if (this.CurrentMediaPlayer == null)
            return false;
        var resumeTcs = new TaskCompletionSource<bool>();
        void OnResumed(object? sender, EventArgs e)
        {
            resumeTcs?.SetResult(true);
            this.CurrentMediaPlayer!.Playing -= OnResumed;
        }
        
        try
        {
            this.CurrentMediaPlayer.Playing += OnResumed;
            this.CurrentMediaPlayer.Play();
            await resumeTcs.Task;
            this.Resumed?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Playing));
        }
        catch (Exception e)
        {
            this.CurrentMediaPlayer.Playing -= OnResumed; // Ensure we don't leak the event handler
            return false;
        }
        
        return true;
    }

    public async ValueTask<bool> StopAsync()
    {
        if (this.CurrentMediaPlayer == null)
            return false;
        
        try
        {
            await this.CurrentMediaPlayer.StopAsync();
            var vlcControl = this.ExternalVideoSurface as IVLCVideoControl;
            if (vlcControl == null)
                return false;
            await vlcControl.PrepareVideoAsync(this);
            this.Stopped?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Stopped));
        }
        catch (Exception e)
        {
            return false;
        }
        
        return true;
    }

    public ValueTask<ChannelState> GetStateAsync()
    {
        if (this.CurrentMediaPlayer == null)
            return new ValueTask<ChannelState>(ChannelState.Invalid);
        var state = this.CurrentMediaPlayer.State switch
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
        if (this.CurrentMediaPlayer == null)
            return new ValueTask<TimeSpan?>(TimeSpan.Zero);
        var pos = this.CurrentMediaPlayer.Time;
        return ValueTask.FromResult<TimeSpan?>(TimeSpan.FromMilliseconds(pos));
    }

    public ValueTask<bool> SetPositionAsync(double positionMs)
    {
        if (this.CurrentMediaPlayer == null)
            return ValueTask.FromResult(false);
        this.CurrentMediaPlayer.SeekTo(TimeSpan.FromMilliseconds(positionMs));
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> SetPositionAsync(TimeSpan position)
    {
        if (this.CurrentMediaPlayer == null)
            return ValueTask.FromResult(false);
        this.CurrentMediaPlayer.SeekTo(position);
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
        if (this.CurrentMediaPlayer == null || this.CurrentMedia == null)
            return new ValueTask<TimeSpan?>(TimeSpan.Zero);
        
        var len = this.CurrentMedia.Duration;
        return ValueTask.FromResult<TimeSpan?>(TimeSpan.FromMilliseconds(len));
    }
    
    public ValueTask<bool> SetExternalVideoSurfaceAsync(IExternalPlayerSurface surface)
    {
        if (this.ExternalVideoSurface == surface)
            return ValueTask.FromResult(true);
        
        if (surface is not IVLCVideoControl vlcControl)
            return ValueTask.FromResult(false);
        
        this.ExternalVideoSurface = vlcControl;
        this.CurrentMediaPlayer = vlcControl.MediaPlayer;
        this.ExternalVideoSurfaceChanged?.InvokeAndForget(this, new ExternalPlayerSurfaceChangedEventArgs(surface));
        return ValueTask.FromResult(true);
    }


    public ValueTask<bool> DestroyAsync()
    {
        this.CurrentMedia?.Dispose();
        return ValueTask.FromResult(true);
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
        if (this.CurrentMediaPlayer != null)
            this.CurrentMediaPlayer.AddSlave(MediaSlaveType.Audio, uri.AbsoluteUri, true);
        this.AudioSlaves.Add(audioItem);
        this.AudioSlaveAdded?.InvokeAndForget(this, new ChannelAudioSlaveAddedEventArgs(audioItem));
        return true;
    }

    public ValueTask<bool> RemoveAudioSlaveAsync(AudioItem audioItem)
    {
        var path = audioItem.GetCachePathAsync().Result;
        if (string.IsNullOrWhiteSpace(path))
            return ValueTask.FromResult(false);
        var uri = new Uri(path);
        if (this.CurrentMediaPlayer != null)
        {
            //TODO: Find a way to remove the slave
        }
        this.AudioSlaves.Remove(audioItem);
        this.AudioSlaveRemoved?.InvokeAndForget(this, new ChannelAudioSlaveRemovedEventArgs(audioItem));
        return ValueTask.FromResult(true);
    }
}