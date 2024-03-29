using Manager.Shared.Entities;
using Manager.Shared.Events.Video;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.Video;

/// <summary>
/// Some backend services support video stream selection in their channels.
/// </summary>
public interface IChannelSupportsVideoStreamSelection
{
    #region Events

    /// <summary>
    /// Fired when the selected video stream of this channel changes.
    /// </summary>
    public event AsyncEventHandler<SelectedVideoStreamChangedEventArgs>? SelectedVideoStreamChanged;

    #endregion
    
    /// <summary>
    /// Gets all selectable video streams of this channel.
    /// </summary>
    public ValueTask<SelectableMediaStream[]?> GetSelectableVideoStreamsAsync();
    /// <summary>
    /// Sets the selected video stream of this channel.
    /// </summary>
    public ValueTask<bool> SetSelectedVideoStreamAsync(SelectableMediaStream? stream);
    /// <summary>
    /// Gets the selected video stream of this channel.
    /// </summary>
    public ValueTask<SelectableMediaStream?> GetSelectedVideoStreamAsync();
}