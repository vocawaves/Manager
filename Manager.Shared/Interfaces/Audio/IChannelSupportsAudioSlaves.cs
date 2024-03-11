using Manager.Shared.Entities;
using Manager.Shared.Events.Audio;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.Audio;

public interface IChannelSupportsAudioSlaves
{
    public event AsyncEventHandler<ChannelAudioSlaveAddedEventArgs>? AudioSlaveAdded;
    public event AsyncEventHandler<ChannelAudioSlaveRemovedEventArgs>? AudioSlaveRemoved;
    
    public List<AudioItem> AudioSlaves { get; }
    public ValueTask<bool> AddAudioSlaveAsync(AudioItem audioItem);
    public ValueTask<bool> RemoveAudioSlaveAsync(AudioItem audioItem);
    
}