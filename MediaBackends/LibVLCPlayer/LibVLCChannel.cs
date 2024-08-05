using System.Diagnostics;
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

public class LibVLCChannel : IVideoChannel, IAudioChannel, IChannelSupportsAudioSlaves
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

    public event AsyncEventHandler<ChannelVolumeChangedEventArgs>? ChannelVolumeChanged;
    public event AsyncEventHandler<ChannelDeviceChangedEventArgs>? ChannelDeviceChanged;

    public IBackendService AssociatedBackend { get; }
    public MediaPlayer Player { get; }
    public MediaItem MediaItem { get; }

    public TimeSpan? Position { get; private set; }
    public TimeSpan? Length { get; private set; }

    public List<PositionTrigger> PositionTriggers { get; } = new();
    private readonly List<PositionTrigger> _positionTriggersAlreadyTriggered = new();

    public List<MediaItem> AudioSlaves { get; } = new();
    public IExternalPlayerSurface? ExternalVideoSurface { get; private set; }

    private long _lastDeltaTimeMeasure = 0;
    private System.Timers.Timer _positionTimer = new(1000.0 / 60.0);


    private List<Action> _pendingEvents = new();
    private bool _isEventThreadRunning = false;
    private Thread? _eventThread;

    private object _actionLock = new();

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
        SetupEventThread();
    }

    private void SetupEventThread()
    {
        _eventThread = new Thread(() =>
        {
            _isEventThreadRunning = true;
            while (_isEventThreadRunning)
            {
                if (_pendingEvents.Count == 0)
                {
                    Thread.Sleep(1000 / 60);
                    continue;
                }

                try
                {
                    if (_pendingEvents[0] == null!) //Technically this should never happen, but it does
                    {
                        _pendingEvents.RemoveAt(0);
                        continue;
                    }

                    _pendingEvents[0].Invoke();
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "Failed to execute action");
                    continue;
                }

                _pendingEvents.RemoveAt(0);
            }
        });
        _eventThread.Start();
    }

    private void SetupEventHandlers()
    {
        this.Player.EndReached += (sender, args) =>
        {
            this._lastDeltaTimeMeasure = Stopwatch.GetTimestamp();
            this._positionTimer.Stop();
            this._pendingEvents.Add(() =>
                this.Ended?.InvokeAndForget(this, EventArgs.Empty));
            this._pendingEvents.Add(() =>
                this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Stopped)));
            this._logger?.LogInformation("Media playback ended");
        };
        this.Player.TimeChanged += (sender, args) => { this._lastDeltaTimeMeasure = Stopwatch.GetTimestamp(); };
        this._positionTimer.Elapsed += (sender, args) =>
        {
            var pos = TimeSpan.FromMilliseconds(this.Player.Time);
            this.Position = pos + TimeSpan.FromTicks(Stopwatch.GetTimestamp() - _lastDeltaTimeMeasure);
            this._pendingEvents.Add(() =>
                this.PositionChanged?.InvokeAndForget(this, new ChannelPositionChangedEventArgs(pos)));
            foreach (var trigger in this.PositionTriggers)
            {
                if (trigger.Position <= pos && !_positionTriggersAlreadyTriggered.Contains(trigger))
                {
                    this._pendingEvents.Add(() =>
                        this.PositionTriggerInvoked?.InvokeAndForget(this,
                            new ChannelPositionTriggerInvokedEventArgs(trigger)));
                    _positionTriggersAlreadyTriggered.Add(trigger);
                }
            }
        };
    }

    public async ValueTask<bool> PlayAsync()
    {
        var playTcs = new TaskCompletionSource<bool>();
        var vOutTcs = new TaskCompletionSource<bool>();

        var state = await this.GetStateAsync();
        if (state is ChannelState.Playing or ChannelState.Paused)
            await this.StopAsync();
        var pos = await this.GetPositionAsync();
        if (pos != null)
        {
            var couldStop = await this.StopAsync(true);
            var couldPos = await this.SetPositionAsync(TimeSpan.Zero);
        }

        void OnPlaying(object? sender, EventArgs e)
        {
            this.Player.Playing -= OnPlaying;
            playTcs.SetResult(true);
            _logger?.LogDebug("Playing event received");
        }

        void OnVOut(object? sender, EventArgs e)
        {
            this.Player.Vout -= OnVOut;
            vOutTcs.SetResult(true);
            _logger?.LogDebug("Vout event received");
        }

        try
        {
            this.Player.Playing += OnPlaying;
            if (this.MediaItem.ItemType != ItemType.Audio)
                this.Player.Vout += OnVOut;
            _logger?.LogDebug("Playing media");
            lock (_actionLock)
            {
                _logger?.LogDebug("Lock taken by PlayAsync");
                this.Player.Play();
            }

            _logger?.LogDebug("Lock released by PlayAsync");
            _logger?.LogDebug("Play command sent");
            if (this.MediaItem.ItemType == ItemType.Audio)
                await playTcs.Task.ConfigureAwait(false);
            else
                await Task.WhenAll(playTcs.Task, vOutTcs.Task);
            _logger?.LogDebug("Play task completed");
            _positionTimer.Start();
            _pendingEvents.Add(() =>
                this.Playing?.InvokeAndForget(this, EventArgs.Empty));
            _pendingEvents.Add(() =>
                this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Playing)));
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
        var state = await this.GetStateAsync().ConfigureAwait(false);
        if (state != ChannelState.Playing)
        {
            this._logger?.LogError("Cannot pause media that is not playing");
            return false;
        }

        void OnPaused(object? sender, EventArgs e)
        {
            this.Player.Paused -= OnPaused;
            pauseTcs?.SetResult(true);
            _logger?.LogDebug("Paused event received");
        }

        try
        {
            this.Player.Paused += OnPaused;
            _logger?.LogDebug("Pausing media");

            lock (_actionLock)
            {
                _logger?.LogDebug("Lock taken by PauseAsync");
                this.Player.Pause();
            }

            _logger?.LogDebug("Lock released by PauseAsync");
            _logger?.LogDebug("Pause command sent");
            await pauseTcs.Task.ConfigureAwait(false);
            _logger?.LogDebug("Pause task completed");
            _lastDeltaTimeMeasure = Stopwatch.GetTimestamp();
            _positionTimer.Stop();
            this._pendingEvents.Add(() =>
                this.Paused?.InvokeAndForget(this, EventArgs.Empty));
            this._pendingEvents.Add(() =>
                this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Paused)));
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
        var state = await this.GetStateAsync();
        _logger?.LogDebug("Current state is {0}", state);
        if (state != ChannelState.Paused)
        {
            this._logger?.LogError("Cannot resume media that is not paused");
            return false;
        }

        void OnResumed(object? sender, EventArgs e)
        {
            this.Player.Playing -= OnResumed;
            resumeTcs?.SetResult(true);
            _logger?.LogDebug("Resumed event received");
        }

        try
        {
            this.Player.Playing += OnResumed;
            _logger?.LogDebug("Resuming media");

            lock (_actionLock)
            {
                _logger?.LogDebug("Lock taken by ResumeAsync");
                this.Player.Play();
            }

            _logger?.LogDebug("Lock released by ResumeAsync");
            _logger?.LogDebug("Resume command sent");
            await resumeTcs.Task.ConfigureAwait(false);
            _logger?.LogDebug("Resume task completed");
            _positionTimer.Start();
            this._pendingEvents.Add(() =>
                this.Resumed?.InvokeAndForget(this, EventArgs.Empty));
            this._pendingEvents.Add(() =>
                this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Playing)));
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to resume media");
            this.Player.Playing -= OnResumed; // Ensure we don't leak the event handler
            return false;
        }

        return true;
    }

    public ValueTask<bool> StopAsync()
        => StopAsync(false);


    private async ValueTask<bool> StopAsync(bool quiet)
    {
        var stopTcs = new TaskCompletionSource<bool>();

        var state = await this.GetStateAsync();
        if (state == ChannelState.Stopped)
            return true;

        void OnStopped(object? sender, EventArgs e)
        {
            this.Player.Stopped -= OnStopped;
            stopTcs?.SetResult(true);
            _logger?.LogDebug("Stopped event received");
        }

        try
        {
            this.Player.Stopped += OnStopped;
            _logger?.LogDebug("Stopping media");

            lock (_actionLock)
            {
                _logger?.LogDebug("Lock taken by StopAsync");
                this.Player.Stop();
            }

            _logger?.LogDebug("Lock released by StopAsync");
            _logger?.LogDebug("Stop command sent");
            await stopTcs.Task;
            _logger?.LogDebug("Stop task completed");
            _lastDeltaTimeMeasure = Stopwatch.GetTimestamp();
            _positionTimer.Stop();
            if (!quiet)
            {
                _pendingEvents.Add(() =>
                    this.Stopped?.InvokeAndForget(this, EventArgs.Empty));
                _pendingEvents.Add(() =>
                    this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Stopped)));
            }
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
            VLCState.Ended => ChannelState.Ended,
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
        var pos = TimeSpan.FromMilliseconds(this.Player.Time +
                                            TimeSpan.FromTicks(Stopwatch.GetTimestamp() - _lastDeltaTimeMeasure)
                                                .TotalMilliseconds);
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
        _logger?.LogDebug("Lock taken by SetPositionAsync");
        var tryResult = true;
        lock (_actionLock)
        {
            var action = Task.Run(() =>
            {
                this.Player.Time = (long)positionMs;
            });
            if (!action.Wait(TimeSpan.FromMilliseconds(100)))
            {
                this._logger?.LogError("Failed to set media position");
                tryResult = false;
            }
        }
        _logger?.LogDebug("Lock released by SetPositionAsync");

        if (preSeekState == ChannelState.Playing)
        {
            var couldResume = await this.ResumeAsync().ConfigureAwait(false);
            if (!couldResume)
            {
                this._logger?.LogError("Failed to resume media");
                return false;
            }
        }

        if (!tryResult)
        {
            this._logger?.LogError("Failed to set media position");
            return false;
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
        this._pendingEvents.Add(() =>
            this.ExternalVideoSurfaceChanged?.InvokeAndForget(this,
                new ExternalPlayerSurfaceChangedEventArgs(this.ExternalVideoSurface)));
        this._logger?.LogInformation("External video surface set to this channels player");
        return true;
    }


    public ValueTask<bool> DestroyAsync()
    {
        _isEventThreadRunning = false;
        _eventThread?.Join();
        _eventThread = null;
        this.Player.Media?.Dispose();
        this.Player.Dispose();
        this._positionTimer.Stop();
        this._positionTimer.Dispose();
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

        lock (_actionLock)
        {
            this.Player.AddSlave(MediaSlaveType.Audio, uri.AbsoluteUri, true);
        }

        this.AudioSlaves.Add(audioItem);
        this._pendingEvents.Add(() =>
            this.AudioSlaveAdded?.InvokeAndForget(this, new ChannelAudioSlaveAddedEventArgs(audioItem)));
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
        this._pendingEvents.Add(() =>
            this.AudioSlaveRemoved?.InvokeAndForget(this, new ChannelAudioSlaveRemovedEventArgs(audioItem)));
        return true;
    }

    public ValueTask<float?> GetVolumeAsync()
    {
        return ValueTask.FromResult<float?>(this.Player.Volume / 100.0f);
    }

    public ValueTask<bool> SetVolumeAsync(float volume)
    {
        if (volume == 0f)
        {
            this.Player.Mute = true;
            this._pendingEvents.Add(() =>
                this.ChannelVolumeChanged?.InvokeAndForget(this, new ChannelVolumeChangedEventArgs(0f)));
            return ValueTask.FromResult(true);
        }

        this.Player.Volume = (int)(volume * 100.0f);
        this._pendingEvents.Add(() =>
            this.ChannelVolumeChanged?.InvokeAndForget(this, new ChannelVolumeChangedEventArgs(volume)));
        return ValueTask.FromResult(true);
    }

    public ValueTask<AudioDevice?> GetDeviceAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetDeviceAsync(AudioDevice device)
    {
        throw new NotImplementedException();
    }
}