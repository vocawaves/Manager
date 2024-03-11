using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.Data;

public interface IImageDataSource
{
    public ValueTask<ImageItem?> GetImageItemAsync(string uri);
    public ValueTask<ImageItem[]?> GetImageItemPlaylistAsync(string uri);
}