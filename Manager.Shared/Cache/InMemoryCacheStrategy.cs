using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;

namespace Manager.Shared.Cache;

public class InMemoryCacheStrategy : ICacheStrategy
{
    private readonly Dictionary<MediaItem, byte[]> _cachedItems = new();

    private readonly ILogger<InMemoryCacheStrategy>? _logger;
    
    public InMemoryCacheStrategy(ILogger<InMemoryCacheStrategy>? logger = null, string[]? options = null)
    {
        _logger = logger;
    }

    public static ICacheStrategy? Create(ILogger<ICacheStrategy>? logger = null, string[]? options = null)
    {
        return new InMemoryCacheStrategy(logger as ILogger<InMemoryCacheStrategy>);
    }

    public ValueTask<bool> CheckForExistingCacheAsync(MediaItem mediaItem, string cacheName)
    {
        if (!this._cachedItems.ContainsKey(mediaItem))
            return ValueTask.FromResult(false);
        mediaItem.SetCacheState(CacheState.Cached);
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> CacheAsync(MediaItem mediaItem, byte[] data, string cacheName)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> CacheAsync(MediaItem mediaItem, Stream data, string cacheName)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> CacheAsync(MediaItem mediaItem, string path, string cacheName)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> RemoveAsync(MediaItem mediaItem)
    {
        throw new NotImplementedException();
    }

    public ValueTask<string?> GetCachedPathAsync(MediaItem mediaItem)
    {
        throw new NotImplementedException();
    }

    public ValueTask<Stream?> GetCachedStreamAsync(MediaItem mediaItem)
    {
        throw new NotImplementedException();
    }
}