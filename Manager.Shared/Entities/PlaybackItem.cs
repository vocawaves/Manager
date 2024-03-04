using Manager.Shared.Enums;
using Manager.Shared.Interfaces;
using Manager.Shared.Interfaces.Data;

namespace Manager.Shared.Entities;

/// <summary>
/// Holds metadata info for a PlaybackItem of whatever supported format.
/// The displayed title should be as follows:
/// - if there is no title Metadata, use the filename(+ext) or some sort of ID if URL
/// - if there is a title but no custom title, use Title
/// - if custom title is, use that with an * at the start (maybe title as tooltip)
/// </summary>
public class PlaybackItem
{
    public IDataService AssociatedDataService { get; }

    public string Title { get; }

    public string? CustomTitle { get; set; }

    public string Artist { get; }

    public TimeSpan Duration { get; }

    public byte[]? Thumbnail { get; internal set; }

    public string? ThumbnailExtension { get; internal set; }

    public string? ThumbnailMimeType { get; internal set; }

    public string OwnerPath { get; init; }

    public string Extension { get; init; }

    public string MimeType { get; init; }

    public ulong OwnerId { get; init; }

    public bool IsCached { get; set; }
    
    public Dictionary<string, string> Metadata { get; } = new();

    public PlaybackItem(IDataService dataService, string ownerPath, string extension, string mimeType, string title,
        string artist, TimeSpan duration, ulong ownerId)
    {
        this.AssociatedDataService = dataService;
        this.Title = title;
        this.Artist = artist;
        this.Duration = duration;
        this.OwnerId = ownerId;
        this.OwnerPath = ownerPath;
        this.Extension = extension;
        this.MimeType = mimeType;
    }

    public void SetThumbnail(byte[] thumbnail, string extension, string mimeType)
    {
        Thumbnail = thumbnail;
        ThumbnailExtension = extension;
        ThumbnailMimeType = mimeType;
    }

    public ValueTask<string?> GetCachedPathAsync() => this.AssociatedDataService.CacheStrategy.GetCachedPathAsync(this);

    public ValueTask<Stream?> GetCachedStreamAsync() =>
        this.AssociatedDataService.CacheStrategy.GetCachedStreamAsync(this);
}