using Manager.Shared.Entities;
using Manager.Shared.Events.Subtitle;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.Subtitle;

/// <summary>
/// Some backend services support subtitle stream selection in their channels.
/// </summary>
public interface IChannelSupportsSubtitleStreamSelection
{
    #region Events
    
    /// <summary>
    /// Fired when the selected subtitle stream of this channel changes.
    /// </summary>
    public event AsyncEventHandler<SelectedSubtitleStreamChangedEventArgs>? SelectedSubtitleStreamChanged;

    #endregion
    
    /// <summary>
    /// Gets all selectable subtitle streams of this channel.
    /// </summary>
    public ValueTask<SelectableMediaStream[]?> GetSelectableSubtitleStreamsAsync();
    /// <summary>
    /// Sets the selected subtitle stream of this channel.
    /// </summary>
    public ValueTask<bool> SetSelectedSubtitleStreamAsync(SelectableMediaStream? stream);
    /// <summary>
    /// Gets the selected subtitle stream of this channel.
    /// </summary>
    public ValueTask<SelectableMediaStream?> GetSelectedSubtitleStreamAsync();
}