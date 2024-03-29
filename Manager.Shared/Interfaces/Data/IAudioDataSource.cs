using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.Data;

/// <summary>
/// Sort of a filter interface that the corresponding data source can get audio items.
/// </summary>
public interface IAudioDataSource
{
    /// <summary>
    /// Gets an audio item from the data source.
    /// Or null if it doesn't exist.
    /// </summary>
    public ValueTask<AudioItem?> GetAudioItemAsync(string uri);
    /// <summary>
    /// Gets an audio item playlist from the data source.
    /// For example, a folder, a playlist file or a YouTube playlist.
    /// </summary>
    public ValueTask<AudioItem[]?> GetAudioItemPlaylistAsync(string uri);
}