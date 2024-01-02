using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces;

public interface IChannelSupportsAudioStreamSelection
{
    public ValueTask<SelectableMediaStream[]?> GetSelectableAudioStreamsAsync();
    public ValueTask<bool> SetSelectedAudioStreamAsync(SelectableMediaStream? stream);
}