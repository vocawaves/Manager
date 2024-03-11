using Manager.Shared.Interfaces.Data;

namespace Manager.Shared.Entities;

public class VideoItem : MediaItem
{
    public TimeSpan Duration { get; }
    public byte[]? ThumbnailData { get; }
    public string? ThumbnailMimeType { get; }

    public VideoItem(IDataService source, ulong ownerId, string sourcePath, string pathTitle, TimeSpan duration) : base(
        source, ownerId, sourcePath, pathTitle)
    {
        Duration = duration;
    }

    public VideoItem(IDataService source, ulong ownerId, string sourcePath, string pathTitle, TimeSpan duration,
        byte[] thumbnailData, string thumbnailMimeType) : base(source, ownerId, sourcePath, pathTitle)
    {
        Duration = duration;
        ThumbnailData = thumbnailData;
        ThumbnailMimeType = thumbnailMimeType;
    }
}