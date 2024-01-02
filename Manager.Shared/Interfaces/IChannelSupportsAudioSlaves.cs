using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces;

public interface IChannelSupportsAudioSlaves
{
    public List<PlayItem> AudioSlaves { get; }
    public ValueTask<bool> AddAudioSlaveAsync(PlayItem playItem);
    public ValueTask<bool> RemoveAudioSlaveAsync(PlayItem playItem);
    
}