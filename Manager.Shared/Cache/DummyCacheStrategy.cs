using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.General;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;

namespace Manager.Shared.Cache;

/// <summary>
/// Dont cache anything. Just return the MediaItems source path or stream to it.
/// </summary>
public class DummyCacheStrategy : ICacheStrategy
{
    #region IManagerComponent
    public ComponentManager ComponentManager { get; }
    public string Name { get; }
    public ulong Parent { get; }
    
    #endregion
    
    private readonly ILogger<DummyCacheStrategy>? _logger;
    
    public DummyCacheStrategy(ComponentManager componentManager, string name, ulong parent)
    {
        ComponentManager = componentManager;
        Name = name;
        Parent = parent;
        _logger = componentManager.CreateLogger<DummyCacheStrategy>();
    }
    
    public ValueTask<bool> CheckForExistingCacheAsync(MediaItem mediaItem, string cacheName)
    {
        _logger?.LogDebug("Checking for existing cache for {PathTitle}", mediaItem.PathTitle);
        if (!File.Exists(mediaItem.SourcePath))
        {
            _logger?.LogDebug("Cache file {CacheName} for {PathTitle} does not exist", cacheName, mediaItem.PathTitle);
            return ValueTask.FromResult(false);
        }

        mediaItem.SetCacheState(CacheState.Cached);
        _logger?.LogDebug("Cache for {PathTitle} exists", mediaItem.PathTitle);
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> CacheAsync(MediaItem mediaItem, byte[] data, string cacheName)
    {
        _logger?.LogDebug("Caching {PathTitle} to {CacheName}", mediaItem.PathTitle, cacheName);
        throw new NotSupportedException("DummyCacheStrategy does not support byte[] caching. Only file paths and FileStreams.");
    }

    public ValueTask<bool> CacheAsync(MediaItem mediaItem, Stream data, string cacheName)
    {
        _logger?.LogDebug("Caching {PathTitle} to {CacheName}", mediaItem.PathTitle, cacheName);
        if (data is not FileStream)
        {
            _logger?.LogDebug("Cache file {CacheName} for {PathTitle} does not exist", cacheName, mediaItem.PathTitle);
            throw new NotSupportedException(
                "DummyCacheStrategy does not support Stream caching. Only file paths and FileStreams.");
        }

        mediaItem.SetCacheState(CacheState.Cached);
        _logger?.LogDebug("Cache for {PathTitle} exists", mediaItem.PathTitle);
        return ValueTask.FromResult(true);

    }

    public ValueTask<bool> CacheAsync(MediaItem mediaItem, string path, string cacheName)
    {
        _logger?.LogDebug("Caching {PathTitle} to {CacheName}", mediaItem.PathTitle, cacheName);
        if (!File.Exists(path))
        {
            _logger?.LogDebug("Cache file {CacheName} for {PathTitle} does not exist", cacheName, mediaItem.PathTitle);
            return ValueTask.FromResult(false);
        }

        mediaItem.SetCacheState(CacheState.Cached);
        _logger?.LogDebug("Cache for {PathTitle} exists", mediaItem.PathTitle);
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> RemoveAsync(MediaItem mediaItem)
    {
        mediaItem.SetCacheState(CacheState.NotCached);
        _logger?.LogDebug("Removed cache for {PathTitle}", mediaItem.PathTitle);
        return ValueTask.FromResult(true);
    }

    public ValueTask<string?> GetCachedPathAsync(MediaItem mediaItem)
    {
        _logger?.LogDebug("Getting cached path for {PathTitle}", mediaItem.PathTitle);
        if (File.Exists(mediaItem.SourcePath))
        {
            _logger?.LogDebug("Cache for {PathTitle} exists", mediaItem.PathTitle);
            return ValueTask.FromResult<string?>(mediaItem.SourcePath);
        }
        _logger?.LogDebug("Cache for {PathTitle} does not exist", mediaItem.PathTitle);
        return ValueTask.FromResult<string?>(null);
    }

    public ValueTask<Stream?> GetCachedStreamAsync(MediaItem mediaItem)
    {
        _logger?.LogDebug("Getting cached stream for {PathTitle}", mediaItem.PathTitle);
        if (File.Exists(mediaItem.SourcePath))
        {
            _logger?.LogDebug("Getting cached stream for {PathTitle}", mediaItem.PathTitle);
            return ValueTask.FromResult<Stream?>(new FileStream(mediaItem.SourcePath, FileMode.Open, FileAccess.Read));
        }

        _logger?.LogDebug("Cache for {PathTitle} does not exist", mediaItem.PathTitle);
        return ValueTask.FromResult<Stream?>(null);
    }
}