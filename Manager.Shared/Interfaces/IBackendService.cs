using Manager.Shared.Delegates;
using Manager.Shared.Entities;
using Manager.Shared.Enums;

namespace Manager.Shared.Interfaces;

public interface IBackendService
{
    public event BackendServiceChannelCreatedEventHandler? ChannelCreated;
    public event BackendServiceChannelDestroyedEventHandler? ChannelDestroyed;
    public event BackendServiceChannelStateChangedEventHandler? ChannelStateChanged;
    public event BackendServiceChannelPositionChangedEventHandler? ChannelPositionChanged;
    
    public ValueTask<IMediaChannel?> CreateChannelAsync(PlayItem playItem, Action<PlayItem>? onEnded = null);
    public ValueTask<bool> DestroyChannelAsync(IMediaChannel channel);
    
    public ValueTask<bool> PlayChannelAsync(IMediaChannel channel);
    public ValueTask<bool> PauseChannelAsync(IMediaChannel channel);
    public ValueTask<bool> ResumeChannelAsync(IMediaChannel channel);
    public ValueTask<bool> StopChannelAsync(IMediaChannel channel);
    
    public ValueTask<ChannelState?> GetChannelStateAsync(IMediaChannel channel);
    public ValueTask<bool> SetChannelStateAsync(IMediaChannel channel, ChannelState state);
    
    public ValueTask<TimeSpan?> GetChannelPositionAsync(IMediaChannel channel);
    public ValueTask<bool> SetChannelPositionAsync(IMediaChannel channel, double positionMs);
    public ValueTask<bool> SetChannelPositionAsync(IMediaChannel channel, TimeSpan position);
    
    public ValueTask<TimeSpan?> GetChannelLengthAsync(IMediaChannel channel);
}