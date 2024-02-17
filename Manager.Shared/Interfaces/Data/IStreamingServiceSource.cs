using Manager.Shared.Entities;
using Manager.Shared.Enums;

namespace Manager.Shared.Interfaces.Data;

public interface IStreamingServiceSource : IDataService
{
    public ValueTask<PlaybackItem[]?> GetPlaylistAsync(string url, int limit = 0);
    public ValueTask<PlaybackItem[]?> GetAuthorItemsAsync(string url, int limit = 0);
    public ValueTask<PlaybackItem[]?> SearchAsync(string query, SearchFilter filter = SearchFilter.None);
}