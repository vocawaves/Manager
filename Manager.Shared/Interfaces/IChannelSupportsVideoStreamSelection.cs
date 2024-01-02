using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces;

public interface IChannelSupportsVideoStreamSelection
{
    public ValueTask<SelectableMediaStream[]?> GetSelectableVideoStreamsAsync();
    public ValueTask<bool> SetSelectedVideoStreamAsync(SelectableMediaStream? stream);
}