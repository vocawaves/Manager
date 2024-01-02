using Manager.Shared.Enums;
using Manager.Shared.Interfaces;

namespace Manager.Shared.Entities;

public class PlayItem
{
    public required IDataService AssociatedDataService { get; init; }
    
    public string Title { get; internal set; } = string.Empty;
    
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