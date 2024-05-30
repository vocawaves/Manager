using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;

namespace Manager.Shared.Cache;

public class FolderCacheStrategy : ICacheStrategy
{
    private readonly string _cacheDirectory;
    
    private readonly Dictionary<MediaItem, string> _cachedItems = new();
    private readonly ILogger<FolderCacheStrategy>? _logger;

    public FolderCacheStrategy(ILogger<FolderCacheStrategy>? logger = null, string[]? options = null)
    {
        _logger = logger;
        _cacheDirectory = options?.FirstOrDefault() ?? Path.Combine(Directory.GetCurrentDirectory(), "ManagerCache");
        if (!Directory.Exists(_cacheDirectory))
            Directory.CreateDirectory(_cacheDirectory);
    }

    public static ICacheStrategy? Create(ILogger<ICacheStrategy>? logger = null, string[]? options = null)
    {
        return new FolderCacheStrategy(logger as ILogger<FolderCacheStrategy>, options);
    }

    public ValueTask<bool> CheckForExistingCacheAsync(MediaItem mediaItem, string cacheName)
    {
        var cachePath = Path.Combine(this._cacheDirectory, cacheName);
        if (File.Exists(cachePath))
        {
            this._logger?.LogDebug("Cache file for {PathTitle} already exists in cache directory", mediaItem.PathTitle);
            _cachedItems.TryAdd(mediaItem, cacheName);
            mediaItem.SetCacheState(CacheState.Cached);
            return ValueTask.FromResult(true);
        }
        else if (_cachedItems.TryGetValue(mediaItem, out var path) && path == cacheName)
        {
            this._logger?.LogDebug("Cache file for {PathTitle} already exists in cache Dictionary, but not on disk", mediaItem.PathTitle);
            mediaItem.SetCacheState(CacheState.NotCached);
            return ValueTask.FromResult(false);
        }
        
        this._logger?.LogDebug("Cache file for {PathTitle} does not exist in cache directory", mediaItem.PathTitle);
        return ValueTask.FromResult(false);
    }
    
    public async ValueTask<bool> CacheAsync(MediaItem mediaItem, byte[] data, string cacheName)
    {
        this._logger?.LogDebug("Caching {PathTitle} to {CacheName}", mediaItem.PathTitle, cacheName);
        var alreadyCached = await CheckForExistingCacheAsync(mediaItem, cacheName);
        if (alreadyCached)
            return true;
        
        mediaItem.SetCacheState(CacheState.DiskCaching);
        var cachePath = Path.Combine(this._cacheDirectory, cacheName);
        await using var fileStream = File.Create(cachePath);
        await using var memoryStream = new MemoryStream(data);
        var buffer = new byte[4096];
        var totalBytes = memoryStream.Length;
        var totalWritten = 0L;
        int bytesRead;
        while ((bytesRead = memoryStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);
            totalWritten += bytesRead;
            var progress = (int) ((totalWritten / (double) totalBytes) * 100);
            mediaItem.SetCacheProgress(progress);
        }
        
        mediaItem.SetCacheState(CacheState.Cached);
        _cachedItems[mediaItem] = cacheName;
        _logger?.LogDebug("Cached {PathTitle} to {CacheName}", mediaItem.PathTitle, cacheName);
        return true;
    }

    public async ValueTask<bool> CacheAsync(MediaItem mediaItem, Stream data, string cacheName)
    {
        this._logger?.LogDebug("Caching {PathTitle} to {CacheName}", mediaItem.PathTitle, cacheName);
        var alreadyCached = await CheckForExistingCacheAsync(mediaItem, cacheName);
        if (alreadyCached)
            return true;
        
        mediaItem.SetCacheState(CacheState.DiskCaching);
        var cachePath = Path.Combine(this._cacheDirectory, cacheName);
        await using var fileStream = File.Create(cachePath);
        var buffer = new byte[4096];
        var totalBytes = data.Length;
        var totalWritten = 0L;
        int bytesRead;
        while ((bytesRead = await data.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);
            totalWritten += bytesRead;
            var progress = (int) ((totalWritten / (double) totalBytes) * 100);
            mediaItem.SetCacheProgress(progress);
        }
        
        mediaItem.SetCacheState(CacheState.Cached);
        _cachedItems[mediaItem] = cacheName;
        _logger?.LogDebug("Cached {PathTitle} to {CacheName}", mediaItem.PathTitle, cacheName);
        return true;
    }

    public async ValueTask<bool> CacheAsync(MediaItem mediaItem, string path, string cacheName)
    {
        this._logger?.LogDebug("Caching {PathTitle} to {CacheName}", mediaItem.PathTitle, cacheName);
        var alreadyCached = await CheckForExistingCacheAsync(mediaItem, cacheName);
        if (alreadyCached)
            return true;
        
        mediaItem.SetCacheState(CacheState.DiskCaching);
        var cachePath = Path.Combine(this._cacheDirectory, cacheName);
        await using var fileStream = File.Create(cachePath);
        await using var sourceStream = File.OpenRead(path);
        var buffer = new byte[4096];
        var totalBytes = sourceStream.Length;
        var totalWritten = 0L;
        int bytesRead;
        while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);
            totalWritten += bytesRead;
            var progress = (int) ((totalWritten / (double) totalBytes) * 100);
            mediaItem.SetCacheProgress(progress);
        }
        
        mediaItem.SetCacheState(CacheState.Cached);
        _cachedItems[mediaItem] = cacheName;
        _logger?.LogDebug("Cached {PathTitle} to {CacheName}", mediaItem.PathTitle, cacheName);
        return true;
    }

    public ValueTask<bool> RemoveAsync(MediaItem mediaItem)
    {
        if (!this._cachedItems.Remove(mediaItem, out var path))
        {
            this._logger?.LogDebug("Cache file for {PathTitle} does not exist in cache Dictionary", mediaItem.PathTitle);
            return ValueTask.FromResult(false);
        }

        var cachePath = Path.Combine(this._cacheDirectory, path);
        if (!File.Exists(cachePath))
        {
            this._logger?.LogDebug("Cache file for {PathTitle} does not exist on disk", mediaItem.PathTitle);
            return ValueTask.FromResult(false);
        }
        
        File.Delete(cachePath);
        this._logger?.LogDebug("Removed cache file for {PathTitle}", mediaItem.PathTitle);
        return ValueTask.FromResult(true);
    }

    public ValueTask<string?> GetCachedPathAsync(MediaItem mediaItem)
    {
        if (!this._cachedItems.TryGetValue(mediaItem, out var path))
            return ValueTask.FromResult<string?>(null);

        this._logger?.LogDebug("Returning {PathTitle} from {CachePath}", mediaItem.PathTitle, path);
        var cachePath = Path.Combine(this._cacheDirectory, path);
        return ValueTask.FromResult<string?>(cachePath);
    }

    public ValueTask<Stream?> GetCachedStreamAsync(MediaItem mediaItem)
    {
        if (!this._cachedItems.TryGetValue(mediaItem, out var path))
            return ValueTask.FromResult<Stream?>(null);

        this._logger?.LogDebug("Opening {PathTitle} from {CachePath}", mediaItem.PathTitle, path);
        try
        {
            var cachePath = Path.Combine(this._cacheDirectory, path);
            return ValueTask.FromResult<Stream?>(File.OpenRead(cachePath));
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to open {PathTitle} from {CachePath}", mediaItem.PathTitle, path);
            return ValueTask.FromResult<Stream?>(null);
        }
    }
}