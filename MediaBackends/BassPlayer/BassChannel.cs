using CommunityToolkit.Mvvm.ComponentModel;
using ManagedBass;
using Manager2.Shared;
using Manager2.Shared.Enums;
using Microsoft.Extensions.Logging;
using Manager2.Shared.BaseModels;
using Timer = System.Timers.Timer;

// ReSharper disable once CheckNamespace
namespace Manager.MediaBackends;

public partial class BassChannel : MediaChannel
{
    [ObservableProperty] private int _channel;
    
    private readonly Timer _positionTimer;
    private readonly Lock _positionLock = new();
    
    public BassChannel(int channel, PlaybackBackend backend, MediaItem media, ILogger<MediaChannel>? logger = default) : base(backend, media, logger)
    {
        _channel = channel;
        _positionTimer = new Timer(TimeSpan.FromMilliseconds(10));
        _positionTimer.Elapsed += UpdatePosition;
        InitEndEvent();
    }

    private void InitEndEvent()
    {
        var couldSetSync = Bass.ChannelSetSync(Channel, SyncFlags.End, 0, (a, b, c, d) =>
        {
            Logger?.LogDebug("Channel {0} ended", Channel);
            State = ChannelState.Ended;
            _positionTimer.Stop();
        });
        if (couldSetSync == 0) 
            Logger?.LogError("Failed to set end sync on channel {0}: {1}", Channel, Bass.LastError);
        else
            Logger?.LogDebug("Set end sync on channel {0}", Channel);
    }

    private void UpdatePosition(object? s, EventArgs e)
    {
        var position = Bass.ChannelGetPosition(Channel);
        if (position == -1)
        {
            Logger?.LogError("Failed to get position of channel {0}: {1}", Channel, Bass.LastError);
            return;
        }
        var posAsSeconds = Bass.ChannelBytes2Seconds(Channel, position);
        //Logger?.LogDebug("Position of channel {0} is {1}", Channel, posAsSeconds);
        lock (_positionLock)
        {
            Position = TimeSpan.FromSeconds(posAsSeconds);   
        }
    }

    public override ValueTask<bool> PlayAsync()
    {
        if (State == ChannelState.Playing)
        {
            Logger?.LogInformation("Already playing, restarting");
        }
        
        var couldPlay = Bass.ChannelPlay(Channel, true);
        if (!couldPlay)
        {
            Logger?.LogError("Failed to play channel {0}: {1}", Channel, Bass.LastError);
            return new ValueTask<bool>(false);
        }
        
        State = ChannelState.Playing;
        _positionTimer.Start();
        Logger?.LogDebug("Playing channel {0}", Channel);
        return new ValueTask<bool>(true);
    }

    public override ValueTask<bool> PauseAsync()
    {
        if (State == ChannelState.Paused)
        {
            Logger?.LogWarning("Already paused");
            return new ValueTask<bool>(true);
        }

        var couldPause = Bass.ChannelPause(Channel);
        if (!couldPause)
        {
            Logger?.LogError("Failed to pause channel {0}: {1}", Channel, Bass.LastError);
            return new ValueTask<bool>(false);
        }
        
        State = ChannelState.Paused;
        _positionTimer.Stop();
        Logger?.LogDebug("Paused channel {0}", Channel);
        return new ValueTask<bool>(true);
    }

    public override ValueTask<bool> ResumeAsync()
    {
        if (State == ChannelState.Playing)
        {
            Logger?.LogWarning("Already playing");
            return new ValueTask<bool>(true);
        }

        var couldResume = Bass.ChannelPlay(Channel);
        if (!couldResume)
        {
            Logger?.LogError("Failed to resume channel {0}: {1}", Channel, Bass.LastError);
            return new ValueTask<bool>(false);
        }
        
        State = ChannelState.Playing;
        _positionTimer.Start();
        Logger?.LogDebug("Resumed channel {0}", Channel);
        return new ValueTask<bool>(true);
    }

    public override ValueTask<bool> StopAsync()
    {
        if (State == ChannelState.Stopped)
        {
            Logger?.LogWarning("Already stopped");
            return new ValueTask<bool>(true);
        }

        var couldStop = Bass.ChannelStop(Channel);
        if (!couldStop)
        {
            Logger?.LogError("Failed to stop channel {0}: {1}", Channel, Bass.LastError);
            return new ValueTask<bool>(false);
        }
        
        State = ChannelState.Stopped;
        _positionTimer.Stop();
        Logger?.LogDebug("Stopped channel {0}", Channel);
        return new ValueTask<bool>(true);
    }

    public override ValueTask<bool> SetPositionAsync(TimeSpan position)
    {
        var couldSetPosition = Bass.ChannelSetPosition(Channel, (long) position.TotalMilliseconds);
        if (!couldSetPosition)
        {
            Logger?.LogError("Failed to set position of channel {0}: {1}", Channel, Bass.LastError);
            return new ValueTask<bool>(false);
        }
        
        Position = position;
        Logger?.LogDebug("Set position of channel {0} to {1}", Channel, position);
        return new ValueTask<bool>(true);
    }

    public override ValueTask<bool> SetStateAsync(ChannelState state)
    {
        switch (state)
        {
            case ChannelState.Playing:
                return PlayAsync();
            case ChannelState.Paused:
                return PauseAsync();
            case ChannelState.Stopped:
                return StopAsync();
            case ChannelState.Ended:
                Logger?.LogError("Cannot set state to Ended");
                return new ValueTask<bool>(false);
            default:
                Logger?.LogError("Unknown state {0}", state);
                return new ValueTask<bool>(false);
        }
    }
}