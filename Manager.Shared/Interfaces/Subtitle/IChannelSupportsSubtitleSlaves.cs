using Manager.Shared.Entities;
using Manager.Shared.Events.Subtitle;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.Subtitle;

public interface IChannelSupportsSubtitleSlaves
{
    #region Events

    public event AsyncEventHandler<SubtitleSlaveAddedEventArgs>? SubtitleSlaveAdded;
    public event AsyncEventHandler<SubtitleSlaveRemovedEventArgs>? SubtitleSlaveRemoved;

    #endregion

    public List<SubtitleItem> SubtitleSlaves { get; }
    public ValueTask<bool> AddSubtitleSlaveAsync(SubtitleItem subtitleItem);
    public ValueTask<bool> RemoveSubtitleSlaveAsync(SubtitleItem subtitleItem);
}