using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;

namespace Manager.Shared.Entities;

public class VideoItem : MediaItem
{
    public TimeSpan Duration { get; }
    public byte[]? ThumbnailData { get; }
    public string? ThumbnailMimeType { get; }

    public VideoItem(ILoggerFactory lf, IDataService source, ulong ownerId, string sourcePath, string pathTitle,
        TimeSpan duration) : base(lf,
        source, ownerId, sourcePath, pathTitle)
    {
        Duration = duration;
    }

    public VideoItem(ILoggerFactory lf, IDataService source, ulong ownerId, string sourcePath, string pathTitle,
        TimeSpan duration,
        byte[] thumbnailData, string thumbnailMimeType) : base(lf, source, ownerId, sourcePath, pathTitle)
    {
        Duration = duration;
        ThumbnailData = thumbnailData;
        ThumbnailMimeType = thumbnailMimeType;
    }
}