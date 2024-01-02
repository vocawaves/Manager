using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces;

public interface IBackendSupportsSubtitleStreamSelection
{
    public ValueTask<SelectableMediaStream[]?> GetSelectableSubtitleStreamsAsync(ISubtitleChannel mediaChannel);
    public ValueTask<bool> SetSelectedSubtitleStreamAsync(ISubtitleChannel mediaChannel, SelectableMediaStream? stream);
}