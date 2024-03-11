using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.Data;

public interface ISubtitleDataSource
{
    public ValueTask<SubtitleItem?> GetSubtitleItemAsync(string uri);
    public ValueTask<SubtitleItem[]?> GetSubtitleItemPlaylistAsync(string uri);
}