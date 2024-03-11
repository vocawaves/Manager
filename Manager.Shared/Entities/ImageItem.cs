using Manager.Shared.Interfaces.Data;

namespace Manager.Shared.Entities;

public class ImageItem : MediaItem
{
    public int Width { get; }
    public int Height { get; }

    public ImageItem(IDataService dataService, ulong ownerId, string sourcePath, string pathTitle, int width,
        int height) : base(dataService, ownerId, sourcePath, pathTitle)
    {
        Width = width;
        Height = height;
    }
}