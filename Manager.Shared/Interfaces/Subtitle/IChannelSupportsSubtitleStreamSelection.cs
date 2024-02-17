using Manager.Shared.Entities;
using Manager.Shared.Events.Subtitle;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.Subtitle;

public interface IChannelSupportsSubtitleStreamSelection
{
    #region Events
    
    public event AsyncEventHandler<SelectedSubtitleStreamChangedEventArgs>? SelectedSubtitleStreamChanged;

    #endregion
    
    public ValueTask<SelectableMediaStream[]?> GetSelectableSubtitleStreamsAsync();
    public ValueTask<bool> SetSelectedSubtitleStreamAsync(SelectableMediaStream? stream);
    public ValueTask<SelectableMediaStream?> GetSelectedSubtitleStreamAsync();
}