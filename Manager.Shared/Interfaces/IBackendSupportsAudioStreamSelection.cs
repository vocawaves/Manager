using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces;

public interface IBackendSupportsAudioStreamSelection
{
    public ValueTask<SelectableMediaStream[]?> GetSelectableAudioStreamsAsync(IAudioChannel mediaChannel);
    public ValueTask<bool> SetSelectedAudioStreamAsync(IAudioChannel mediaChannel, SelectableMediaStream? stream);
}