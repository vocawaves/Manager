using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;

namespace Manager.Shared.Entities;

/// <summary>
/// An extension of <see cref="MediaItem"/> that represents a video item.
/// </summary>
public class VideoItem : MediaItem
{
    /// <summary>
    /// Duration of the video item.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Thumbnail data in bytes. (if available)
    /// </summary>
    public byte[]? ThumbnailData { get; }

    /// <summary>
    /// Mime type of the thumbnail. (if available)
    /// </summary>
    public string? ThumbnailMimeType { get; }

    public VideoItem(IDataService source, ulong ownerId, string sourcePath, string pathTitle,
        TimeSpan duration, ILogger<VideoItem> logger) : base(source, ownerId, sourcePath, pathTitle, logger)
    {
        Duration = duration;
    }

    public VideoItem(IDataService source, ulong ownerId, string sourcePath, string pathTitle,
        TimeSpan duration,
        byte[] thumbnailData, string thumbnailMimeType, ILogger<VideoItem> logger) : base(source, ownerId, sourcePath,
        pathTitle, logger)
    {
        Duration = duration;
        ThumbnailData = thumbnailData;
        ThumbnailMimeType = thumbnailMimeType;
    }
}