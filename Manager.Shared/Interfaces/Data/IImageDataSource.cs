using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.Data;

/// <summary>
/// Sort of a filter interface that the corresponding data source can get image items.
/// </summary>
public interface IImageDataSource
{
    /// <summary>
    /// Gets an image item from the data source.
    /// </summary>
    public ValueTask<ImageItem?> GetImageItemAsync(string uri);
    /// <summary>
    /// Get multiple image items from the data source.
    /// </summary>
    public ValueTask<ImageItem[]?> GetImageItemPlaylistAsync(string uri);
}