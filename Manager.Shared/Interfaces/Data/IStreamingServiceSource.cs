using Manager.Shared.Entities;
using Manager.Shared.Enums;

namespace Manager.Shared.Interfaces.Data;

public interface IStreamingServiceSource : IDataService
{
    public ValueTask<MediaItem[]?> GetPlaylistAsync(string url, ItemType type, int limit = 0);
    public ValueTask<MediaItem[]?> GetAuthorItemsAsync(string url, ItemType itemType, int limit = 0);
    public ValueTask<MediaItem[]?> SearchAsync(string query, ItemType itemType, SearchFilter filter = SearchFilter.None);
}