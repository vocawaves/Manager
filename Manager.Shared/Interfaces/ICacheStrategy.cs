using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces;

public interface ICacheStrategy
{
    public ValueTask<bool> CacheAsync(PlayItem playItem, byte[] data);
    public ValueTask<bool> CacheAsync(PlayItem playItem, Stream data);
    public ValueTask<bool> CacheAsync(PlayItem playItem, string path);
    
    public ValueTask<bool> RemoveAsync(PlayItem playItem);
    
    public ValueTask<string?> GetCachedPathAsync(PlayItem playItem);
    
    public ValueTask<Stream?> GetCachedStreamAsync(PlayItem playItem);
}