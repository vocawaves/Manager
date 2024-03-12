using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;

namespace Manager.Shared.Entities;

public class ImageItem : MediaItem
{
    public int Width { get; }
    public int Height { get; }

    public ImageItem(ILoggerFactory lf, IDataService dataService, ulong ownerId, string sourcePath, string pathTitle, int width,
        int height) : base(lf, dataService, ownerId, sourcePath, pathTitle)
    {
        Width = width;
        Height = height;
    }
}