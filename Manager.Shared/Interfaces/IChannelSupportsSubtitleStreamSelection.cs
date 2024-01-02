using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces;

public interface IChannelSupportsSubtitleStreamSelection
{
    public ValueTask<SelectableMediaStream[]?> GetSelectableSubtitleStreamsAsync();
    public ValueTask<bool> SetSelectedSubtitleStreamAsync(SelectableMediaStream? stream);
}