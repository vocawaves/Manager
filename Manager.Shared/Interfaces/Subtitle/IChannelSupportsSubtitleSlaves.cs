using Manager.Shared.Entities;
using Manager.Shared.Events.Subtitle;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.Subtitle;

/// <summary>
/// Defines a channel that supports subtitle slaves.
/// For example, some backends may support multiple subtitle streams like libVLC.
/// </summary>
public interface IChannelSupportsSubtitleSlaves
{
    #region Events

    /// <summary>
    /// Fired when a subtitle slave is added.
    /// </summary>
    public event AsyncEventHandler<SubtitleSlaveAddedEventArgs>? SubtitleSlaveAdded;
    /// <summary>
    /// Fired when a subtitle slave is removed.
    /// </summary>
    public event AsyncEventHandler<SubtitleSlaveRemovedEventArgs>? SubtitleSlaveRemoved;

    #endregion

    /// <summary>
    /// List of subtitle slaves.
    /// Should not be modified from the outside.
    /// </summary>
    public List<SubtitleItem> SubtitleSlaves { get; }
    /// <summary>
    /// Adds a subtitle slave to the channel.
    /// Also fires the <see cref="SubtitleSlaveAdded"/> event.
    /// </summary>
    public ValueTask<bool> AddSubtitleSlaveAsync(SubtitleItem subtitleItem);
    /// <summary>
    /// Removes a subtitle slave from the channel.
    /// Also fires the <see cref="SubtitleSlaveRemoved"/> event.
    /// </summary>
    public ValueTask<bool> RemoveSubtitleSlaveAsync(SubtitleItem subtitleItem);
}