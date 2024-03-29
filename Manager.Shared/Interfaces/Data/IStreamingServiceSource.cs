using Manager.Shared.Entities;
using Manager.Shared.Enums;

namespace Manager.Shared.Interfaces.Data;

/// <summary>
/// Intended for streaming services like YouTube, SoundCloud, etc.
/// This service works in combination with the IAudioDataService, IVideoDataService and others to get MediaItems in a standardised way.
/// </summary>
public interface IStreamingServiceSource : IDataService
{
    /// <summary>
    /// Get MediaItem from a playlist. (or something similar to a playlist)
    /// </summary>
    public ValueTask<MediaItem[]?> GetPlaylistAsync(string url, ItemType type, int limit = 0);
    /// <summary>
    /// Get MediaItem from an author. (or something similar to an author)
    /// </summary>
    public ValueTask<MediaItem[]?> GetAuthorItemsAsync(string url, ItemType itemType, int limit = 0);
    /// <summary>
    /// Search for MediaItems on the streaming service.
    /// </summary>
    public ValueTask<MediaItem[]?> SearchAsync(string query, ItemType itemType, SearchFilter filter = SearchFilter.None);
}