using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.Data;

/// <summary>
/// Gets miscellaneous items. Maybe controlled by the user what extensions are allowed.
/// </summary>
public interface IMiscDataSource
{
    /// <summary>
    /// Gets a miscellaneous item from the data source.
    /// </summary>
    public ValueTask<MediaItem?> GetMiscItemAsync(string uri);
    /// <summary>
    /// Gets multiple miscellaneous items from the data source.
    /// </summary>
    public ValueTask<MediaItem[]?> GetMiscItemPlaylistAsync(string uri);
}