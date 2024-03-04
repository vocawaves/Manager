using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.Data;

public interface ICacheStrategy
{
    public ValueTask<bool> CacheAsync(PlaybackItem playbackItem, byte[] data, string cacheName);
    public ValueTask<bool> CacheAsync(PlaybackItem playbackItem, Stream data, string cacheName);
    public ValueTask<bool> CacheAsync(PlaybackItem playbackItem, string path, string cacheName);

    public ValueTask<bool> RemoveAsync(PlaybackItem playbackItem);

    public ValueTask<string?> GetCachedPathAsync(PlaybackItem playbackItem);
    public ValueTask<Stream?> GetCachedStreamAsync(PlaybackItem playbackItem);
}