using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.Data;

public interface ICacheStrategy
{
    public ValueTask<bool> CacheAsync(MediaItem mediaItem, byte[] data, string cacheName);
    public ValueTask<bool> CacheAsync(MediaItem mediaItem, Stream data, string cacheName);
    public ValueTask<bool> CacheAsync(MediaItem mediaItem, string path, string cacheName);

    public ValueTask<bool> RemoveAsync(MediaItem mediaItem);

    public ValueTask<string?> GetCachedPathAsync(MediaItem mediaItem);
    public ValueTask<Stream?> GetCachedStreamAsync(MediaItem mediaItem);
}