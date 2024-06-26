﻿using System.Diagnostics;
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

    private Stopwatch _positionDeltaTimer = new();
    private System.Timers.Timer _positionTimer = new(TimeSpan.FromMilliseconds(1000.0 / 60.0));

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
            this._positionDeltaTimer.Stop();
            this._positionTimer.Stop();
            this.Ended?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Ended));
            this._logger?.LogInformation("Media playback ended");
        };
        this.Player.TimeChanged += (sender, args) =>
        {
            this._positionDeltaTimer.Restart();
        };
        this._positionTimer.Elapsed += (sender, args) =>
        {
            var pos = TimeSpan.FromMilliseconds(this.Player.Time);
            this.Position = pos + this._positionDeltaTimer.Elapsed;
            this.PositionChanged?.InvokeAndForget(this, new ChannelPositionChangedEventArgs(pos));
            foreach (var trigger in this.PositionTriggers)
            {
                if (trigger.Position <= pos && !_positionTriggersAlreadyTriggered.Contains(trigger))
                {
                    this.PositionTriggerInvoked?.InvokeAndForget(this, new ChannelPositionTriggerInvokedEventArgs(trigger));
                    _positionTriggersAlreadyTriggered.Add(trigger);
                }
            }
        };
    }

    //public ValueTask<bool> PrepareForPlayAsync(IExternalPlayerSurface surface)
    //{
    //    throw new NotImplementedException();
    //}

    public async ValueTask<bool> PlayAsync()
    {
        var playTcs = new TaskCompletionSource<bool>();
        var voutTcs = new TaskCompletionSource<bool>();

        void OnPlaying(object? sender, EventArgs e)
        {
            playTcs.SetResult(true);
            this.Player.Playing -= OnPlaying;
        }

        void OnVout(object? sender, EventArgs e)
        {
            voutTcs.SetResult(true);
            this.Player.Vout -= OnVout;
        }

        try
        {
            this.Player.Playing += OnPlaying;
            this.Player.Vout += OnVout;
            this.Player.Play();
            await Task.WhenAll(playTcs.Task, voutTcs.Task).ConfigureAwait(false);
            _positionTimer.Start();
            this.Playing?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Playing));
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to play media");
            this.Player.Playing -= OnPlaying; // Ensure we don't leak the event handler
            return false;
        }

        await Task.Yield();
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
            this.Player.Paused += OnPaused;
            this.Player.Pause();
            await pauseTcs.Task.ConfigureAwait(false);
            _positionDeltaTimer.Stop();
            _positionTimer.Stop();
            this.Paused?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Paused));
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to pause media");
            this.Player.Paused -= OnPaused; // Ensure we don't leak the event handler
            return false;
        }

        await Task.Yield();
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
            this.Player.Playing += OnResumed;
            this.Player.Play();
            await resumeTcs.Task.ConfigureAwait(false);
            _positionTimer.Start();
            this.Resumed?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Playing));
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to resume media");
            this.Player.Playing -= OnResumed; // Ensure we don't leak the event handler
            return false;
        }

        await Task.Yield();
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
            this.Player.Stopped += OnStopped;
            this.Player.Stop();
            await stopTcs.Task.ConfigureAwait(false);
            _positionDeltaTimer.Stop();
            _positionTimer.Stop();
            this.Stopped?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Stopped));
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to stop media");
            return false;
        }

        await Task.Yield();
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
        var pos = TimeSpan.FromMilliseconds(this.Player.Time + this._positionDeltaTimer.Elapsed.TotalMilliseconds);
        return ValueTask.FromResult<TimeSpan?>(pos);
    }

    public async ValueTask<bool> SetPositionAsync(double positionMs)
    {
        var preSeekState = await this.GetStateAsync().ConfigureAwait(false);
        var preSeekPos = await this.GetPositionAsync().ConfigureAwait(false);
        if (preSeekPos == null)
        {
            this._logger?.LogError("Failed to get current media position");
            return false;
        }

        if (preSeekState == ChannelState.Playing)
        {
            var couldPause = await this.PauseAsync().ConfigureAwait(false);
            if (!couldPause)
            {
                this._logger?.LogError("Failed to pause media");
                return false;
            }
        }

        this.Player.Time = (long)positionMs;

        if (preSeekState == ChannelState.Playing)
        {
            var couldResume = await this.ResumeAsync().ConfigureAwait(false);
            if (!couldResume)
            {
                this._logger?.LogError("Failed to resume media");
                return false;
            }
        }

        this._logger?.LogInformation("Media position set to {0}", positionMs);
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

    public async ValueTask<bool> SetExternalVideoSurfaceAsync(IExternalPlayerSurface surface)
    {
        if (this.ExternalVideoSurface == surface)
        {
            this._logger?.LogInformation("Surface is already set to the player");
            return true;
        }

        if (surface is not IVLCVideoControl vlcControl)
        {
            this._logger?.LogError("Surface is not a VLC video control");
            return false;
        }

        var couldSet = await vlcControl.SetPlayerToControlAsync(this.Player);
        if (!couldSet)
        {
            this._logger?.LogError("Failed to set player to control");
            return false;
        }

        this.ExternalVideoSurface = vlcControl;
        this.ExternalVideoSurfaceChanged?.InvokeAndForget(this, new ExternalPlayerSurfaceChangedEventArgs(surface));
        this._logger?.LogInformation("External video surface set to this channels player");
        return true;
    }


    public ValueTask<bool> DestroyAsync()
    {
        this.Player.Media?.Dispose();
        this.Player.Dispose();
        this._positionTimer.Stop();
        this._positionTimer.Dispose();
        this._positionDeltaTimer.Stop();
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