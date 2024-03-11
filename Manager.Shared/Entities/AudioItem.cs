using Manager.Shared.Interfaces.Data;

namespace Manager.Shared.Entities;

public class AudioItem : MediaItem
{
    public string? Title { get; }
    public string? Artist { get; }

    public TimeSpan Duration { get; }

    public byte[]? AlbumArt { get; }
    public string? AlbumArtMimeType { get; }
    

    public AudioItem(IDataService dataService, ulong ownerId, string sourcePath, string pathTitle, string title,
        string artist, TimeSpan duration) : base(dataService, ownerId, sourcePath, pathTitle)
    {
        Title = title;
        Artist = artist;
        Duration = duration;
    }

    public AudioItem(IDataService dataService, ulong ownerId, string sourcePath, string pathTitle, string title,
        string artist, TimeSpan duration, byte[] albumArt, string albumArtMimeType) : base(dataService, ownerId,
        sourcePath, pathTitle)
    {
        Title = title;
        Artist = artist;
        Duration = duration;
        AlbumArt = albumArt;
        AlbumArtMimeType = albumArtMimeType;
    }
    
    public override async ValueTask DisposeAsync()
    {
        if (this.IsCached)
            await this.RemoveFromCacheAsync();
    }
}