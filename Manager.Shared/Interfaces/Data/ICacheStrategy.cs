using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.Data;

public interface ICacheStrategy
{
    public ValueTask<bool> CacheAsync(PlaybackItem playbackItem, byte[] data);
    public ValueTask<bool> CacheAsync(PlaybackItem playbackItem, Stream data);
    public ValueTask<bool> CacheAsync(PlaybackItem playbackItem, string path);
    
    public ValueTask<bool> RemoveAsync(PlaybackItem playbackItem);
    
    public ValueTask<string?> GetCachedPathAsync(PlaybackItem playbackItem);
    
    public ValueTask<Stream?> GetCachedStreamAsync(PlaybackItem playbackItem);
}