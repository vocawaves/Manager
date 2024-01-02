using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces;

public interface IChannelSupportsSubtitleSlaves
{
    public List<PlayItem> SubtitleSlaves { get; }
    public ValueTask<bool> AddSubtitleSlaveAsync(PlayItem playItem);
    public ValueTask<bool> RemoveSubtitleSlaveAsync(PlayItem playItem);
}