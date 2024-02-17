using Manager.Shared.Entities;
using Manager.Shared.Events.Video;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.Video;

public interface IChannelSupportsVideoStreamSelection
{
    #region Events

    public event AsyncEventHandler<SelectedVideoStreamChangedEventArgs>? SelectedVideoStreamChanged;

    #endregion
    
    public ValueTask<SelectableMediaStream[]?> GetSelectableVideoStreamsAsync();
    public ValueTask<bool> SetSelectedVideoStreamAsync(SelectableMediaStream? stream);
    public ValueTask<SelectableMediaStream?> GetSelectedVideoStreamAsync();
}