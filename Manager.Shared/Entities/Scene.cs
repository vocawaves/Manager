using System.Diagnostics;
using System.Timers;
using Manager.Shared.Enums;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.General;
using Microsoft.Extensions.Logging;
using ValueTaskSupplement;

namespace Manager.Shared.Entities;

public class Scene : IManagerComponent, IAsyncDisposable
{
    public SceneState State { get; private set; }

    public ComponentManager ComponentManager { get; }
    public string Name { get; }
    public ulong Parent { get; }

    private readonly ILogger<Scene>? _logger;

    public List<(TimeSpan Start, IMediaChannel Channel)> Channels { get; } = new();

    public List<(TimeSpan Start, IMediaChannel Channel)> ActiveChannels { get; } = new();
    public List<(TimeSpan Start, IMediaChannel Channel)> EndedChannels { get; } = new();

    private System.Timers.Timer _positionTimer = new(1000.0 / 60.0);
    private long _startTime = -1;
    private long _pauseDelta = 0;
    
    public TimeSpan Position => TimeSpan.FromTicks(Stopwatch.GetTimestamp() - _startTime - _pauseDelta);
    
    public AsyncEventHandler? PositionChanged;

    public Scene(ComponentManager componentManager, string name, ulong parent)
    {
        this.ComponentManager = componentManager;
        this.Name = name;
        this.Parent = parent;
        this._logger = componentManager.CreateLogger<Scene>();
        _positionTimer.Elapsed += HandleTimerElapsed;
    }

    private void HandleTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        //calculate the elapsed time with the pause delta
        var elapsedSpan = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - _startTime - _pauseDelta);
        _ = HandleTimerElapsedAsync(elapsedSpan);
    }

    private async ValueTask HandleTimerElapsedAsync(TimeSpan elapsedSpan)
    {
        //If paused only update the pause delta
        if (State == SceneState.Paused)
        {
            _pauseDelta = elapsedSpan.Ticks;
            return;
        }

        //If stopped, reset the timer
        if (State == SceneState.Stopped)
        {
            return;
        }

        this.PositionChanged?.InvokeAndForget(this, EventArgs.Empty);
        //Start the channels that should start now
        var startChannels = Channels.Where(x => x.Start <= elapsedSpan).ToList();
        foreach (var channel in startChannels)
        {
            channel.Channel.Ended += (sender, args) =>
            {
                ActiveChannels.Remove(channel);
                EndedChannels.Add(channel);
                return ValueTask.CompletedTask;
            };
        }

        foreach (var channel in startChannels)
            ActiveChannels.Add(channel);
        await ValueTaskEx.WhenAll(ActiveChannels.Select(x => x.Channel.PlayAsync()));
    }

    public ValueTask<bool> AddChannelAsync(TimeSpan startTime, IMediaChannel channel)
    {
        Channels.Add((startTime, channel));
        return ValueTask.FromResult(true);
    }

    public async ValueTask<bool> PlayAsync()
    {
        await this.StopAsync();
        var startChannels = Channels.Where(x => x.Start <= TimeSpan.Zero).ToList();
        foreach (var channel in startChannels)
        {
            channel.Channel.Ended += (sender, args) =>
            {
                ActiveChannels.Remove(channel);
                EndedChannels.Add(channel);
                return ValueTask.CompletedTask;
            };
        }

        foreach (var channel in startChannels)
            ActiveChannels.Add(channel);

        await ValueTaskEx.WhenAll(ActiveChannels.Select(x => x.Channel.PlayAsync()));
        _startTime = Stopwatch.GetTimestamp();
        _positionTimer.Start();
        State = SceneState.Playing;
        return true;
    }

    public async ValueTask<bool> PauseAsync()
    {
        if (State != SceneState.Playing)
            return false;

        _positionTimer.Stop();
        State = SceneState.Paused;

        await ValueTaskEx.WhenAll(ActiveChannels.Select(x => x.Channel.PauseAsync()));
        return true;
    }

    public async ValueTask<bool> ResumeAsync()
    {
        if (State != SceneState.Paused)
            return false;

        _startTime = Stopwatch.GetTimestamp() - _pauseDelta;
        _positionTimer.Start();
        State = SceneState.Playing;

        await ValueTaskEx.WhenAll(ActiveChannels.Select(x => x.Channel.ResumeAsync()));
        return true;
    }

    public async ValueTask<bool> StopAsync()
    {
        if (State == SceneState.Stopped)
            return false;

        _positionTimer.Stop();
        _startTime = -1;
        _pauseDelta = 0;
        State = SceneState.Stopped;

        await ValueTaskEx.WhenAll(ActiveChannels.Select(x => x.Channel.StopAsync()));
        return true;
    }


    public async ValueTask DisposeAsync()
    {
        foreach (var channel in Channels)
            await channel.Channel.DisposeAsync();
    }
}