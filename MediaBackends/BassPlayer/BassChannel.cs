using ManagedBass;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.Audio;
using Manager.Shared.Events.General;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.Audio;
using Manager.Shared.Interfaces.General;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace Manager.MediaBackends.BassPlayer;

public class BassChannel : IAudioChannel
{
    private readonly ILogger<BassChannel>? _logger;

    public event AsyncEventHandler? Playing;
    public event AsyncEventHandler? Paused;
    public event AsyncEventHandler? Resumed;
    public event AsyncEventHandler? Stopped;
    public event AsyncEventHandler? Ended;
    public event AsyncEventHandler<ChannelStateChangedEventArgs>? StateChanged;
    public event AsyncEventHandler<ChannelPositionChangedEventArgs>? PositionChanged;
    public event AsyncEventHandler<ChannelPositionTriggerInvokedEventArgs>? PositionTriggerInvoked;
    public event AsyncEventHandler<ChannelVolumeChangedEventArgs>? ChannelVolumeChanged;
    public event AsyncEventHandler<ChannelDeviceChangedEventArgs>? ChannelDeviceChanged;

    public IBackendService AssociatedBackend { get; }
    public AudioItem AudioItem { get; }
    public MediaItem MediaItem => this.AudioItem;
    public List<PositionTrigger> PositionTriggers { get; } = new();
    private readonly Dictionary<PositionTrigger, int> _positionTriggerHandles = new();

    private readonly int _bassChannel;
    //private PeriodicTimer _positionTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(10));
    private Timer _positionTimer = new Timer(TimeSpan.FromMilliseconds(1000.0 / 60.0));

    public BassChannel(IBackendService associatedBackend, AudioItem audioItem,
        int bassChannel, ILogger<BassChannel>? logger = null)
    {
        this._logger = logger;
        this._bassChannel = bassChannel;
        this.AssociatedBackend = associatedBackend;
        this.AudioItem = audioItem;
        SetupEvents();
        this.SetupPosition();
    }

    private void SetupPosition()
    {
        this._positionTimer.Elapsed += (sender, args) =>
        {
            var posAsSeconds = Bass.ChannelBytes2Seconds(_bassChannel, Bass.ChannelGetPosition(_bassChannel));
            this.PositionChanged.InvokeAndForget(this, new ChannelPositionChangedEventArgs(TimeSpan.FromSeconds(posAsSeconds)));
        };
    }

    private void SetupEvents()
    {
        var syncHandle = Bass.ChannelSetSync(_bassChannel, SyncFlags.End, 0, (_, _, _, _) =>
        {
            this.Ended?.InvokeAndForget(this, EventArgs.Empty);
            this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Ended));
            this._positionTimer.Stop();
            this._logger?.LogInformation("End sync for channel {channel} invoked", _bassChannel);
        }, IntPtr.Zero);
        if (syncHandle == 0)
            this._logger?.LogWarning("Failed to set end sync for channel {channel}: {BassLastError}", _bassChannel,
                Bass.LastError);
        else
            this._logger?.LogInformation("Set end sync for channel {channel} at handle {syncHandle}", _bassChannel,
                syncHandle);
    }

    public ValueTask<bool> PlayAsync()
    {
        var success = Bass.ChannelPlay(this._bassChannel, true);
        if (!success)
        {
            this._logger?.LogError("Failed to play channel {channel}: {BassLastError}", _bassChannel, Bass.LastError);
            return ValueTask.FromResult(false);
        }
        
        this.Playing?.InvokeAndForget(this, EventArgs.Empty);
        this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Playing));
        this._positionTimer.Start();
        this._logger?.LogInformation("Playing channel {channel}", _bassChannel);
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> PauseAsync()
    {
        var success = Bass.ChannelPause(_bassChannel);
        if (!success)
        {
            this._logger?.LogError("Failed to pause channel {channel}: {BassLastError}", _bassChannel, Bass.LastError);
            return ValueTask.FromResult(false);
        }
        
        this._positionTimer.Stop();
        this.Paused?.InvokeAndForget(this, EventArgs.Empty);
        this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Paused));
        this._logger?.LogInformation("Paused channel {channel}", _bassChannel);
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> ResumeAsync()
    {
        var success = Bass.ChannelPlay(_bassChannel);
        if (!success)
        {
            this._logger?.LogError("Failed to resume channel {channel}: {BassLastError}", _bassChannel, Bass.LastError);
            return ValueTask.FromResult(false);
        }
        
        this._positionTimer.Start();
        this.Resumed?.InvokeAndForget(this, EventArgs.Empty);
        this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Playing));
        this._logger?.LogInformation("Resumed channel {channel}", _bassChannel);
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> StopAsync()
    {
        var success = Bass.ChannelStop(_bassChannel);
        var resetPos = Bass.ChannelSetPosition(_bassChannel, 0);
        if (!success)
        {
            this._logger?.LogError("Failed to stop channel {channel}: {BassLastError}", _bassChannel, Bass.LastError);
            return ValueTask.FromResult(false);
        }
        if (!resetPos)
        {
            this._logger?.LogError("Failed to reset position for channel {channel}: {BassLastError}", _bassChannel,
                Bass.LastError);
            return ValueTask.FromResult(false);
        }
        
        this.Stopped?.InvokeAndForget(this, EventArgs.Empty);
        this.StateChanged?.InvokeAndForget(this, new ChannelStateChangedEventArgs(ChannelState.Stopped));
        this._positionTimer.Stop();
        this._logger?.LogInformation("Stopped channel {channel}", _bassChannel);
        return ValueTask.FromResult(true);
    }

    public ValueTask<ChannelState> GetStateAsync()
    {
        var state = Bass.ChannelIsActive(_bassChannel);
        switch (state)
        {
            case PlaybackState.Stopped:
                return ValueTask.FromResult(ChannelState.Stopped);
            case PlaybackState.Playing:
                return ValueTask.FromResult(ChannelState.Playing);
            case PlaybackState.Paused:
                return ValueTask.FromResult(ChannelState.Paused);
            default:
                return ValueTask.FromResult(ChannelState.Invalid);
        }
    }

    public ValueTask<bool> SetStateAsync(ChannelState state)
    {
        this._logger?.LogDebug("Setting state for channel {channel} to {state}", _bassChannel, state);
        switch (state)
        {
            case ChannelState.Stopped:
                return this.StopAsync();
            case ChannelState.Playing:
                return this.PlayAsync();
            case ChannelState.Paused:
                return this.PauseAsync();
            case ChannelState.Ended: //Maybe set position to end so it triggers?
            case ChannelState.Invalid:
            default:
                return ValueTask.FromResult(false);
        }
    }

    public ValueTask<TimeSpan?> GetPositionAsync()
    {
        try
        {
            var posAsSeconds = Bass.ChannelBytes2Seconds(_bassChannel, Bass.ChannelGetPosition(_bassChannel));
            this._logger?.LogDebug("Got position for channel {channel}: {position}", _bassChannel, posAsSeconds);
            return ValueTask.FromResult<TimeSpan?>(TimeSpan.FromSeconds(posAsSeconds));
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to get position for channel {channel}: {BassLastError}", _bassChannel,
                Bass.LastError);
            return ValueTask.FromResult<TimeSpan?>(default);
        }
    }

    public ValueTask<bool> SetPositionAsync(double positionMs)
    {
        var msAsSeconds = positionMs / 1000;
        var posAsBytes = Bass.ChannelSeconds2Bytes(_bassChannel, msAsSeconds);
        try
        {
            var success = Bass.ChannelSetPosition(_bassChannel, posAsBytes);
            if (!success)
            {
                this._logger?.LogError("Failed to set position to {position}: {BassLastError}", msAsSeconds,
                    Bass.LastError);
                return ValueTask.FromResult(false);
            }

            this.PositionChanged?.InvokeAndForget(this,
                new ChannelPositionChangedEventArgs(TimeSpan.FromMilliseconds(positionMs)));
            this._logger?.LogInformation("Set position to {position}", msAsSeconds);
            return ValueTask.FromResult(true);
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to set position to {position}: {BassLastError}", msAsSeconds,
                Bass.LastError);
            return ValueTask.FromResult(false);
        }
    }

    public ValueTask<bool> SetPositionAsync(TimeSpan position)
    {
        return SetPositionAsync(position.TotalMilliseconds);
    }

    public ValueTask<PositionTrigger?> AddPositionTriggerAsync(string name, TimeSpan position)
    {
        var trigger = new PositionTrigger(position, name);
        var posAsBytes = Bass.ChannelSeconds2Bytes(_bassChannel, position.TotalSeconds);
        var syncHandle = Bass.ChannelSetSync(_bassChannel, SyncFlags.Position, posAsBytes, (_, _, _, _) =>
        {
            this.PositionTriggerInvoked?.InvokeAndForget(this,
                new ChannelPositionTriggerInvokedEventArgs(trigger));
            this._logger?.LogDebug("Position trigger {trigger.Name} at {trigger.Position} invoked", trigger.Name,
                trigger.Position);
        }, IntPtr.Zero);
        
        if (syncHandle == 0)
        {
            this._logger?.LogError("Failed to set position trigger for channel {channel}: {BassLastError}", _bassChannel,
                Bass.LastError);
            return ValueTask.FromResult<PositionTrigger?>(default);
        }
        
        this.PositionTriggers.Add(trigger);
        this._positionTriggerHandles.Add(trigger, syncHandle);
        this._logger?.LogInformation("Set position trigger for channel {channel} at handle {syncHandle}", _bassChannel,
            syncHandle);
        return ValueTask.FromResult<PositionTrigger?>(trigger);
    }

    public ValueTask<bool> RemovePositionTriggerAsync(PositionTrigger trigger)
    {
        if (!this._positionTriggerHandles.TryGetValue(trigger, out var syncHandle))
        {
            this._logger?.LogError("Failed to remove position trigger {trigger} for channel {channel}: Not found", trigger,
                _bassChannel);
            return ValueTask.FromResult(false);
        }

        var success = Bass.ChannelRemoveSync(_bassChannel, syncHandle);
        if (!success)
        {
            this._logger?.LogError("Failed to remove position trigger {trigger} for channel {channel}: {BassLastError}",
                trigger, _bassChannel, Bass.LastError);
            return ValueTask.FromResult(false);
        }

        this.PositionTriggers.Remove(trigger);
        this._positionTriggerHandles.Remove(trigger);
        this._logger?.LogInformation("Removed position trigger {trigger} for channel {channel}", trigger, _bassChannel);
        return ValueTask.FromResult(true);
    }

    public ValueTask<TimeSpan?> GetLengthAsync()
    {
        try
        {
            var length = Bass.ChannelBytes2Seconds(_bassChannel, Bass.ChannelGetLength(_bassChannel));
            this._logger?.LogDebug("Got length for channel {channel}: {length}", _bassChannel, length);
            return ValueTask.FromResult<TimeSpan?>(TimeSpan.FromSeconds(length));
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to get length for channel {channel}: {BassLastError}", _bassChannel,
                Bass.LastError);
            return ValueTask.FromResult<TimeSpan?>(default);
        }
    }

    public async ValueTask<bool> DestroyAsync()
    {
        this._logger?.LogInformation("Destroying channel {channel}", _bassChannel);
        await this.DisposeAsync();
        return true;
    }

    public ValueTask<float?> GetVolumeAsync()
    {
        try
        {
            var volume = Bass.ChannelGetAttribute(_bassChannel, ChannelAttribute.Volume);
            this._logger?.LogDebug("Got volume for channel {channel}: {volume}", _bassChannel, volume);
            return ValueTask.FromResult<float?>((float)volume);
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to get volume for channel {channel}: {BassLastError}", _bassChannel,
                Bass.LastError);
            return ValueTask.FromResult<float?>(default);
        }
    }

    public ValueTask<bool> SetVolumeAsync(float volume)
    {
        var success = Bass.ChannelSetAttribute(_bassChannel, ChannelAttribute.Volume, volume);
        if (!success)
        {
            this._logger?.LogError("Failed to set volume to {volume}: {BassLastError}", volume, Bass.LastError);
            return ValueTask.FromResult(false);
        }

        this.ChannelVolumeChanged?.InvokeAndForget(this, new ChannelVolumeChangedEventArgs(volume));
        this._logger?.LogInformation("Set volume to {volume}", volume);
        return ValueTask.FromResult(true);
    }

    public ValueTask<AudioDevice?> GetDeviceAsync()
    {
        var device = Bass.ChannelGetDevice(_bassChannel);
        if (device == -1)
        {
            this._logger?.LogError("Failed to get device for channel {channel}: {BassLastError}", _bassChannel,
                Bass.LastError);
            return ValueTask.FromResult<AudioDevice?>(null);
        }

        try
        {
            var deviceInfo = Bass.GetDeviceInfo(device);
            var audioDevice = new AudioDevice((IAudioBackendService)this.AssociatedBackend, deviceInfo.Name,
                device.ToString());
            this._logger?.LogDebug("Got device for channel {channel}: {device}", _bassChannel, audioDevice);
            return ValueTask.FromResult<AudioDevice?>(audioDevice);
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to get device for channel {channel}: {BassLastError}", _bassChannel,
                Bass.LastError);
            return ValueTask.FromResult<AudioDevice?>(null);
        }
    }

    public ValueTask<bool> SetDeviceAsync(AudioDevice device)
    {
        var isValue = int.TryParse(device.Id, out var id);
        if (!isValue)
        {
            this._logger?.LogError("Failed to parse device id from {device}", device);
            return ValueTask.FromResult(false);
        }

        var success = Bass.ChannelSetDevice(this._bassChannel, id);
        if (!success)
        {
            this._logger?.LogError("Failed to set device to {device}: {BassLastError}", device, Bass.LastError);
            return ValueTask.FromResult(false);
        }

        this.ChannelDeviceChanged?.InvokeAndForget(this, new ChannelDeviceChangedEventArgs(device));
        this._logger?.LogInformation("Set device to {device}", device);
        return ValueTask.FromResult(true);
    }

    public ValueTask DisposeAsync()
    {
        this._positionTimer.Dispose();
        Bass.StreamFree(_bassChannel);
        return ValueTask.CompletedTask;
    }
}