using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Manager.Shared.Entities;
using Manager.Shared.Events.General;
using Manager.Shared.Interfaces.Audio;
using Microsoft.Extensions.Logging;

namespace SimplePlayer.API.Entities;

public class SfxChannel
{
    private readonly ILogger<SfxChannel>? _logger;
    public IAudioChannel Channel { get; set; }
    public MediaItem MediaItem { get; set; }
    public SfxState State { get; set; } = SfxState.Stopped;
    
    public SfxChannel(ILogger<SfxChannel>? logger, IAudioChannel channel)
    {
        _logger = logger;
        Channel = channel;
        MediaItem = channel.MediaItem;
        Channel.Ended += ChannelOnEnded;
    }

    private async ValueTask ChannelOnEnded(object sender, EventArgs eventargs)
    {
        _logger?.LogInformation("SFX Ended: {Path}", MediaItem.PathTitle);
        if (State == SfxState.FadingOut)
            await Stop();
        else
        {
            await Stop();
            await Play();
        }
    }

    public async ValueTask Play()
    {
        _logger?.LogInformation("Playing SFX: {Path}", MediaItem.PathTitle);
        Channel.PositionChanged -= ChannelOnPositionChanged; //Just in case
        await Channel.SetVolumeAsync(1);
        await Channel.PlayAsync();
        State = SfxState.Playing;
    }
    
    public ValueTask FadeOut()
    {
        if (State != SfxState.Playing && State != SfxState.FadingOut)
            return ValueTask.CompletedTask;
        _fadeOutStart = Stopwatch.GetTimestamp();
        Channel.PositionChanged += ChannelOnPositionChanged;
        State = SfxState.FadingOut;
        return ValueTask.CompletedTask;
    }

    private long _fadeOutStart;
    private async ValueTask ChannelOnPositionChanged(object sender, ChannelPositionChangedEventArgs eventargs)
    {
        var elapsed = Stopwatch.GetTimestamp() - _fadeOutStart;
        var elapsedSeconds = (double)elapsed / Stopwatch.Frequency;
        if (elapsedSeconds >= 5)
        {
            Channel.PositionChanged -= ChannelOnPositionChanged;
            await Stop();
        }
        else
        {
            var volume = 1 - elapsedSeconds / 5;
            await Channel.SetVolumeAsync((float)volume);
        }
    }

    public async ValueTask Stop()
    {
        _logger?.LogInformation("Stopping SFX: {Path}", MediaItem.PathTitle);
        Channel.PositionChanged -= ChannelOnPositionChanged;
        await Channel.StopAsync();
        State = SfxState.Stopped;
    }
}