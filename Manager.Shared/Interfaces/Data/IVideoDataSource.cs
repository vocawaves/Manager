using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.Data;

/// <summary>
/// Sort of a filter interface that the corresponding data source can get video items.
/// </summary>
public interface IVideoDataSource
{
    /// <summary>
    /// Gets a video item from the data source.
    /// </summary>
    public ValueTask<VideoItem?> GetVideoItemAsync(string uri);
    /// <summary>
    /// Gets multiple video items from the data source.
    /// </summary>
    public ValueTask<VideoItem[]?> GetVideoItemPlaylistAsync(string uri);
}