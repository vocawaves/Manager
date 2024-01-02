using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces;

public interface IBackendSupportsVideoStreamSelection
{
    public ValueTask<SelectableMediaStream[]?> GetSelectableVideoStreamsAsync(IVideoChannel mediaChannel);
    public ValueTask<bool> SetSelectedVideoStreamAsync(IVideoChannel mediaChannel, SelectableMediaStream? stream);
}