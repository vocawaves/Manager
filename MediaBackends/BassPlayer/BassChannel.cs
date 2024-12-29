using CommunityToolkit.Mvvm.ComponentModel;
using ManagedBass;
using Manager2.Shared;
using Manager2.Shared.Enums;
using Microsoft.Extensions.Logging;
using Manager2.Shared.BaseModels;
using Manager2.Shared.Entities;
using Timer = System.Timers.Timer;

// ReSharper disable once CheckNamespace
namespace Manager.MediaBackends;

public partial class BassChannel : MediaChannel
{
    [ObservableProperty] private int _channel;

    private readonly Timer _positionTimer;
    private readonly Lock _positionLock = new();

    public BassChannel(int channel, PlaybackBackend backend, MediaItem media, ILogger<MediaChannel>? logger = default) :
        base(backend, media, logger)
    {
        _channel = channel;
        _positionTimer = new Timer(TimeSpan.FromMilliseconds(10));
        _positionTimer.Elapsed += UpdatePosition;
    }

    internal ReturnResult InitEndEvent()
    {
        var result = new ReturnResult();
        var couldSetSync = Bass.ChannelSetSync(Channel, SyncFlags.End, 0, (a, b, c, d) =>
        {
            Logger?.LogDebug("Channel {0} ended", Channel);
            State = ChannelState.Ended;
            _positionTimer.Stop();
        });
        if (couldSetSync == 0)
        {
            Logger?.LogError("Failed to set end sync on channel {0}: {1}", Channel, Bass.LastError);
            result.Messages.Add(new ReturnMessage(LogLevel.Error,
                $"Failed to set end sync on channel {Channel}: {Bass.LastError}"));
            return result;
        }

        Logger?.LogDebug("Set end sync on channel {0}", Channel);
        result.Success = true;
        return result;
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

    public override ValueTask<ReturnResult> PlayAsync()
    {
        var result = new ReturnResult();
        if (State == ChannelState.Playing)
        {
            Logger?.LogInformation("Already playing, restarting");
        }

        var couldPlay = Bass.ChannelPlay(Channel, true);
        if (!couldPlay)
        {
            Logger?.LogError("Failed to play channel {0}: {1}", Channel, Bass.LastError);
            result.Messages.Add(
                new ReturnMessage(LogLevel.Error, $"Failed to play channel {Channel}: {Bass.LastError}"));
            return ValueTask.FromResult(result);
        }

        State = ChannelState.Playing;
        _positionTimer.Start();
        Logger?.LogDebug("Playing channel {0}", Channel);
        result.Success = true;
        return ValueTask.FromResult(result);
    }

    public override ValueTask<ReturnResult> PauseAsync()
    {
        var result = new ReturnResult();

        if (State == ChannelState.Paused)
        {
            Logger?.LogWarning("Already paused");
            result.Messages.Add(new ReturnMessage(LogLevel.Warning, "Already paused"));
            result.Success = true;
            return ValueTask.FromResult(result);
        }

        var couldPause = Bass.ChannelPause(Channel);
        if (!couldPause)
        {
            Logger?.LogError("Failed to pause channel {0}: {1}", Channel, Bass.LastError);
            result.Messages.Add(new ReturnMessage(LogLevel.Error,
                $"Failed to pause channel {Channel}: {Bass.LastError}"));
            return ValueTask.FromResult(result);
        }

        State = ChannelState.Paused;
        _positionTimer.Stop();
        Logger?.LogDebug("Paused channel {0}", Channel);
        result.Success = true;
        return ValueTask.FromResult(result);
    }

    public override ValueTask<ReturnResult> ResumeAsync()
    {
        var result = new ReturnResult();

        if (State == ChannelState.Playing)
        {
            Logger?.LogWarning("Already playing");
            result.Messages.Add(new ReturnMessage(LogLevel.Warning, "Already playing"));
            result.Success = true;
            return ValueTask.FromResult(result);
        }

        var couldResume = Bass.ChannelPlay(Channel);
        if (!couldResume)
        {
            Logger?.LogError("Failed to resume channel {0}: {1}", Channel, Bass.LastError);
            result.Messages.Add(new ReturnMessage(LogLevel.Error,
                $"Failed to resume channel {Channel}: {Bass.LastError}"));
            return ValueTask.FromResult(result);
        }

        State = ChannelState.Playing;
        _positionTimer.Start();
        Logger?.LogDebug("Resumed channel {0}", Channel);
        result.Success = true;
        return ValueTask.FromResult(result);
    }

    public override ValueTask<ReturnResult> StopAsync()
    {
        var result = new ReturnResult();
        if (State == ChannelState.Stopped)
        {
            Logger?.LogWarning("Already stopped");
            result.Messages.Add(new ReturnMessage(LogLevel.Warning, "Already stopped"));
            result.Success = true;
            return ValueTask.FromResult(result);
        }

        var couldStop = Bass.ChannelStop(Channel);
        if (!couldStop)
        {
            Logger?.LogError("Failed to stop channel {0}: {1}", Channel, Bass.LastError);
            result.Messages.Add(
                new ReturnMessage(LogLevel.Error, $"Failed to stop channel {Channel}: {Bass.LastError}"));
            return ValueTask.FromResult(result);
        }

        State = ChannelState.Stopped;
        _positionTimer.Stop();
        Logger?.LogDebug("Stopped channel {0}", Channel);
        result.Success = true;
        return ValueTask.FromResult(result);
    }

    public override ValueTask<ReturnResult> SetPositionAsync(TimeSpan position)
    {
        var result = new ReturnResult();
        var couldSetPosition = Bass.ChannelSetPosition(Channel, (long)position.TotalMilliseconds);
        if (!couldSetPosition)
        {
            Logger?.LogError("Failed to set position of channel {0}: {1}", Channel, Bass.LastError);
            result.Messages.Add(new ReturnMessage(LogLevel.Error,
                $"Failed to set position of channel {Channel}: {Bass.LastError}"));
            return ValueTask.FromResult(result);
        }

        Position = position;
        Logger?.LogDebug("Set position of channel {0} to {1}", Channel, position);
        result.Success = true;
        return ValueTask.FromResult(result);
    }

    public override ValueTask<ReturnResult> SetStateAsync(ChannelState state)
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
                var result = new ReturnResult();
                result.Messages.Add(new ReturnMessage(LogLevel.Error, "Cannot set state to Ended"));
                return ValueTask.FromResult(result);
            default:
                Logger?.LogError("Unknown state {0}", state);
                var result2 = new ReturnResult();
                result2.Messages.Add(new ReturnMessage(LogLevel.Error, $"Unknown state {state}"));
                return ValueTask.FromResult(result2);
        }
    }

    public override ValueTask DisposeAsync()
    {
        _positionTimer.Stop();
        _positionTimer.Dispose();
        Bass.ChannelStop(Channel);
        Bass.StreamFree(Channel);
        return ValueTask.CompletedTask;
    }
}