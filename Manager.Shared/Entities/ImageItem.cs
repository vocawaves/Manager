using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;

namespace Manager.Shared.Entities;

/// <summary>
/// An extension of <see cref="MediaItem"/> that represents an image item.
/// </summary>
public class ImageItem : MediaItem
{
    /// <summary>
    /// Width of the image.
    /// </summary>
    public int Width { get; }
    /// <summary>
    /// Height of the image.
    /// </summary>
    public int Height { get; }

    public ImageItem(ILoggerFactory lf, IDataService dataService, ulong ownerId, string sourcePath, string pathTitle, int width,
        int height) : base(lf, dataService, ownerId, sourcePath, pathTitle)
    {
        Width = width;
        Height = height;
    }
}