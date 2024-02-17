using Manager.Shared.Entities;
using Manager.Shared.Events.Audio;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.Audio;

public interface IChannelSupportsAudioSlaves
{
    public event AsyncEventHandler<ChannelAudioSlaveAddedEventArgs>? AudioSlaveAdded;
    public event AsyncEventHandler<ChannelAudioSlaveRemovedEventArgs>? AudioSlaveRemoved;
    
    public List<PlaybackItem> AudioSlaves { get; }
    public ValueTask<bool> AddAudioSlaveAsync(PlaybackItem playbackItem);
    public ValueTask<bool> RemoveAudioSlaveAsync(PlaybackItem playbackItem);
    
}