using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;

namespace Manager.Shared.Entities;

/// <summary>
/// An extension of <see cref="MediaItem"/> that represents an audio item.
/// </summary>
public class AudioItem : MediaItem
{
    /// <summary>
    /// Title of the audio item.
    /// </summary>
    public string? Title { get; }
    /// <summary>
    /// Artist of the audio item.
    /// </summary>
    public string? Artist { get; }

    /// <summary>
    /// Duration of the audio item.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Album art in bytes. (if available)
    /// </summary>
    public byte[]? AlbumArt { get; private set; }
    /// <summary>
    /// Mime type of the album art. (if available)
    /// </summary>
    public string? AlbumArtMimeType { get; }

    public AudioItem(IDataService dataService, ulong ownerId, string sourcePath, string pathTitle, string title,
        string artist, TimeSpan duration, ILogger<AudioItem>? logger = null) : base(dataService, ownerId, sourcePath, pathTitle, logger)
    {
        Title = title;
        Artist = artist;
        Duration = duration;
    }

    public AudioItem(IDataService dataService, ulong ownerId, string sourcePath, string pathTitle, string title,
        string artist, TimeSpan duration, byte[] albumArt, string albumArtMimeType, ILogger<AudioItem>? logger = null) : base(dataService, ownerId,
        sourcePath, pathTitle, logger)
    {
        Title = title;
        Artist = artist;
        Duration = duration;
        AlbumArt = albumArt;
        AlbumArtMimeType = albumArtMimeType;
    }
}