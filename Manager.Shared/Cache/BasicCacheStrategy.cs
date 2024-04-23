using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;

namespace Manager.Shared.Cache;

public class BasicCacheStrategy : ICacheStrategy
{
    private readonly string _cacheDirectory;
    
    private readonly Dictionary<MediaItem, string> _cachedItems = new();
    private readonly ILogger<BasicCacheStrategy>? _logger;

    public BasicCacheStrategy(ILoggerFactory? lf = null)
    {
        _logger = lf?.CreateLogger<BasicCacheStrategy>();
        _cacheDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ManagerCache");
        if (!Directory.Exists(_cacheDirectory))
            Directory.CreateDirectory(_cacheDirectory);
    }

    public ValueTask<bool> CheckForOldCacheAsync(MediaItem mediaItem, string cacheName)
    {
        if (this._cachedItems.ContainsKey(mediaItem))
            return ValueTask.FromResult(true);
        
        var cachePath = Path.Combine(_cacheDirectory, cacheName);
        if (!File.Exists(cachePath)) 
            return ValueTask.FromResult(false);
        this._cachedItems.Add(mediaItem, cachePath);
        mediaItem.SetCacheProgress(100);
        mediaItem.SetCacheState(CacheState.Cached);
        return ValueTask.FromResult(true);
    }
    public async ValueTask<bool> CacheAsync(MediaItem mediaItem, byte[] data, string cacheName)
    {
        if (this._cachedItems.ContainsKey(mediaItem))
            return true;
        
        var cachePath = Path.Combine(_cacheDirectory, cacheName);
        this._logger?.LogDebug("Caching {PathTitle} to {CachePath}", mediaItem.PathTitle, cachePath);
        try
        {
            await using var fs = File.Create(cachePath);
            //write in 1MB chunks (or less if last chunk)
            for (int i = 0; i < data.Length; i += 1024 * 1024)
            {
                int length = Math.Min(1024 * 1024, data.Length - i);
                await fs.WriteAsync(data, i, length);
                double progress = (double)i / data.Length * 100;
                mediaItem.SetCacheProgress(progress);
            }
            
            this._cachedItems.Add(mediaItem, cachePath);
            if (mediaItem.CacheProgress < 100)
                mediaItem.SetCacheProgress(100);
            mediaItem.SetCacheState(CacheState.Cached);
            return true;
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to cache {PathTitle} to {CachePath}", mediaItem.PathTitle, cachePath);
            return false;
        }
    }

    public async ValueTask<bool> CacheAsync(MediaItem mediaItem, Stream data, string cacheName)
    {
        if (this._cachedItems.ContainsKey(mediaItem))
            return true;
        
        var cachePath = Path.Combine(_cacheDirectory, cacheName);
        this._logger?.LogDebug("Caching {PathTitle} to {CachePath}", mediaItem.PathTitle, cachePath);
        try
        {
            await using var fs = File.Create(cachePath);
            //try to seek to the beginning of the stream
            if (data.CanSeek)
                data.Seek(0, SeekOrigin.Begin);
            
            //write in 1MB chunks (or less if last chunk)
            var buffer = new byte[1024 * 1024];
            int bytesRead;
            while ((bytesRead = await data.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fs.WriteAsync(buffer, 0, bytesRead);
                double progress = (double)data.Position / data.Length * 100;
                mediaItem.SetCacheProgress(progress);
            }
            this._cachedItems.Add(mediaItem, cachePath);
            if (mediaItem.CacheProgress < 100)
                mediaItem.SetCacheProgress(100);
            mediaItem.SetCacheState(CacheState.Cached);
            return true;
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to cache {PathTitle} to {CachePath}", mediaItem.PathTitle, cachePath);
            return false;
        }
    }

    public async ValueTask<bool> CacheAsync(MediaItem mediaItem, string path, string cacheName)
    {
        if (this._cachedItems.ContainsKey(mediaItem))
            return true;
        
        var cachePath = Path.Combine(_cacheDirectory, cacheName);
        this._logger?.LogDebug("Caching {PathTitle} to {CachePath}", mediaItem.PathTitle, cachePath);
        try
        {
            //File.Copy(path, cachePath, true);
            await using var source = File.OpenRead(path);
            await using var destination = File.Create(cachePath);
            //write in 1MB chunks (or less if last chunk)
            var buffer = new byte[1024 * 1024];
            int bytesRead;
            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead);
                double progress = (double)source.Position / source.Length * 100;
                mediaItem.SetCacheProgress(progress);
            }
            this._cachedItems.Add(mediaItem, cachePath);
            mediaItem.SetCacheState(CacheState.Cached);
            return true;
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to cache {PathTitle} to {CachePath}", mediaItem.PathTitle, cachePath);
            mediaItem.SetCacheState(CacheState.Failed);
            return false;
        }
    }

    public ValueTask<bool> RemoveAsync(MediaItem mediaItem)
    {
        if (!this._cachedItems.ContainsKey(mediaItem))
            return ValueTask.FromResult(false);
        
        var path = this._cachedItems[mediaItem];
        this._logger?.LogDebug("Removing {PathTitle} from {CachePath}", mediaItem.PathTitle, path);
        try
        {
            File.Delete(path);
            this._cachedItems.Remove(mediaItem);
            mediaItem.SetCacheProgress(0);
            mediaItem.SetCacheState(CacheState.NotCached);
            return ValueTask.FromResult(true);
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to remove {PathTitle} from {CachePath}", mediaItem.PathTitle, path);
            return ValueTask.FromResult(false);
        }
    }

    public ValueTask<string?> GetCachedPathAsync(MediaItem mediaItem)
    {
        if (!this._cachedItems.ContainsKey(mediaItem))
            return ValueTask.FromResult<string?>(null);
        
        var path = this._cachedItems[mediaItem];
        this._logger?.LogDebug("Returning {PathTitle} from {CachePath}", mediaItem.PathTitle, path);
        return ValueTask.FromResult<string?>(path);
    }

    public ValueTask<Stream?> GetCachedStreamAsync(MediaItem mediaItem)
    {
        if (!this._cachedItems.ContainsKey(mediaItem))
            return ValueTask.FromResult<Stream?>(null);
        
        var path = this._cachedItems[mediaItem];
        this._logger?.LogDebug("Opening {PathTitle} from {CachePath}", mediaItem.PathTitle, path);
        try
        {
            return ValueTask.FromResult<Stream?>(File.OpenRead(path));
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to open {PathTitle} from {CachePath}", mediaItem.PathTitle, path);
            return ValueTask.FromResult<Stream?>(null);
        }
    }
}