using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;

namespace Manager.Shared.Entities;

public class AudioItem : MediaItem
{
    public string? Title { get; }
    public string? Artist { get; }

    public TimeSpan Duration { get; }

    public byte[]? AlbumArt { get; private set; }
    public string? AlbumArtMimeType { get; }
    

    public AudioItem(ILoggerFactory lf, IDataService dataService, ulong ownerId, string sourcePath, string pathTitle, string title,
        string artist, TimeSpan duration) : base(lf, dataService, ownerId, sourcePath, pathTitle)
    {
        Title = title;
        Artist = artist;
        Duration = duration;
    }

    public AudioItem(ILoggerFactory lf, IDataService dataService, ulong ownerId, string sourcePath, string pathTitle, string title,
        string artist, TimeSpan duration, byte[] albumArt, string albumArtMimeType) : base(lf, dataService, ownerId,
        sourcePath, pathTitle)
    {
        Title = title;
        Artist = artist;
        Duration = duration;
        AlbumArt = albumArt;
        AlbumArtMimeType = albumArtMimeType;
    }
}