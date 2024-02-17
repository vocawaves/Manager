using Manager.Shared.Entities;
using Manager.Shared.Events.Audio;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.Audio;

public interface IChannelSupportsAudioStreamSelection
{
    #region MyRegion

    //SelectedAudioStreamChanged
    public event AsyncEventHandler<ChannelSelectedAudioStreamChangedEventArgs>? SelectedAudioStreamChanged; 

    #endregion
    
    public ValueTask<SelectableMediaStream[]?> GetSelectableAudioStreamsAsync();
    public ValueTask<bool> SetSelectedAudioStreamAsync(SelectableMediaStream? stream);
}