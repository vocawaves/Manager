using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.Data;

public interface IMiscDataSource
{
    public ValueTask<MediaItem?> GetMiscItemAsync(string uri);
    public ValueTask<MediaItem[]?> GetMiscItemPlaylistAsync(string uri);
}