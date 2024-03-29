using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.Data;

/// <summary>
/// Sort of a filter interface that the corresponding data source can get subtitle items.
/// </summary>
public interface ISubtitleDataSource
{
    /// <summary>
    /// Gets a subtitle item from the data source.
    /// </summary>
    public ValueTask<SubtitleItem?> GetSubtitleItemAsync(string uri);
    /// <summary>
    /// Gets multiple subtitle items from the data source.
    /// </summary>
    public ValueTask<SubtitleItem[]?> GetSubtitleItemPlaylistAsync(string uri);
}