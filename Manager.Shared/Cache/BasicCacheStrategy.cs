using Manager.Shared.Entities;
using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;

namespace Manager.Shared.Cache;

public class BasicCacheStrategy : ICacheStrategy
{
    private readonly string _cacheDirectory;
    
    private Dictionary<MediaItem, string> _cachedItems = new();
    
    private readonly ILogger<BasicCacheStrategy> _logger;

    public BasicCacheStrategy(ILoggerFactory lf)
    {
        _logger = lf.CreateLogger<BasicCacheStrategy>();
        _cacheDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ManagerCache");
        if (!Directory.Exists(_cacheDirectory))
            Directory.CreateDirectory(_cacheDirectory);
    }
    
    public async ValueTask<bool> CacheAsync(MediaItem mediaItem, byte[] data, string cacheName)
    {
        if (this._cachedItems.ContainsKey(mediaItem))
            return true;
        
        var cachePath = Path.Combine(_cacheDirectory, cacheName);
        this._logger.LogDebug("Caching {PathTitle} to {CachePath}", mediaItem.PathTitle, cachePath);
        try
        {
            await File.WriteAllBytesAsync(cachePath, data);
            this._cachedItems.Add(mediaItem, cachePath);
            mediaItem.IsCached = true;
            return true;
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Failed to cache {PathTitle} to {CachePath}", mediaItem.PathTitle, cachePath);
            return false;
        }
    }

    public async ValueTask<bool> CacheAsync(MediaItem mediaItem, Stream data, string cacheName)
    {
        if (this._cachedItems.ContainsKey(mediaItem))
            return true;
        
        var cachePath = Path.Combine(_cacheDirectory, cacheName);
        this._logger.LogDebug("Caching {PathTitle} to {CachePath}", mediaItem.PathTitle, cachePath);
        await using var fs = File.Create(cachePath);
        try
        {
            await data.CopyToAsync(fs);
            this._cachedItems.Add(mediaItem, cachePath);
            mediaItem.IsCached = true;
            return true;
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Failed to cache {PathTitle} to {CachePath}", mediaItem.PathTitle, cachePath);
            return false;
        }
    }

    public ValueTask<bool> CacheAsync(MediaItem mediaItem, string path, string cacheName)
    {
        if (this._cachedItems.ContainsKey(mediaItem))
            return ValueTask.FromResult(true);
        
        var cachePath = Path.Combine(_cacheDirectory, cacheName);
        this._logger.LogDebug("Caching {PathTitle} to {CachePath}", mediaItem.PathTitle, cachePath);
        try
        {
            File.Copy(path, cachePath, true);
            this._cachedItems.Add(mediaItem, cachePath);
            mediaItem.IsCached = true;
            return ValueTask.FromResult(true);
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Failed to cache {PathTitle} to {CachePath}", mediaItem.PathTitle, cachePath);
            return ValueTask.FromResult(false);
        }
    }

    public ValueTask<bool> RemoveAsync(MediaItem mediaItem)
    {
        if (!this._cachedItems.ContainsKey(mediaItem))
            return ValueTask.FromResult(false);
        
        var path = this._cachedItems[mediaItem];
        this._logger.LogDebug("Removing {PathTitle} from {CachePath}", mediaItem.PathTitle, path);
        try
        {
            File.Delete(path);
            this._cachedItems.Remove(mediaItem);
            return ValueTask.FromResult(true);
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Failed to remove {PathTitle} from {CachePath}", mediaItem.PathTitle, path);
            return ValueTask.FromResult(false);
        }
    }

    public ValueTask<string?> GetCachedPathAsync(MediaItem mediaItem)
    {
        if (!this._cachedItems.ContainsKey(mediaItem))
            return ValueTask.FromResult<string?>(null);
        
        var path = this._cachedItems[mediaItem];
        this._logger.LogDebug("Returning {PathTitle} from {CachePath}", mediaItem.PathTitle, path);
        return ValueTask.FromResult<string?>(path);
    }

    public ValueTask<Stream?> GetCachedStreamAsync(MediaItem mediaItem)
    {
        if (!this._cachedItems.ContainsKey(mediaItem))
            return ValueTask.FromResult<Stream?>(null);
        
        var path = this._cachedItems[mediaItem];
        this._logger.LogDebug("Opening {PathTitle} from {CachePath}", mediaItem.PathTitle, path);
        try
        {
            return ValueTask.FromResult<Stream?>(File.OpenRead(path));
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Failed to open {PathTitle} from {CachePath}", mediaItem.PathTitle, path);
            return ValueTask.FromResult<Stream?>(null);
        }
    }
}