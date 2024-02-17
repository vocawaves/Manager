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
    public required IDataService AssociatedFileSystemService { get; init; }
    
    public string Title { get; internal set; } = string.Empty;

    public string? CustomTitle { get; set; }
    
    public string Artist { get; internal set; } = string.Empty;

    public TimeSpan Duration { get; internal set; }

    public byte[]? Thumbnail { get; internal set; }
    
    public string? ThumbnailExtension { get; internal set; }
    
    public string? ThumbnailMimeType { get; internal set; }

    public required string OwnerPath { get; init; }

    public required string Extension { get; init; }
    
    public required string MimeType { get; init; }

    public required ulong OwnerId { get; init; }
    
    public required ICacheStrategy CacheStrategy { get; init; }

    public CacheState CacheState { get; internal set; } = CacheState.NotCached;
}