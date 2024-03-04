using Manager.Shared.Entities;
using Manager.Shared.Interfaces.Data;

namespace Manager.Shared.Cache;

public class BasicCacheStrategy : ICacheStrategy
{
    private readonly string _cacheDirectory;
    
    private Dictionary<PlaybackItem, string> _cachedItems = new();

    public BasicCacheStrategy()
    {
        _cacheDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ManagerCache");
        if (!Directory.Exists(_cacheDirectory))
            Directory.CreateDirectory(_cacheDirectory);
    }
    
    public async ValueTask<bool> CacheAsync(PlaybackItem playbackItem, byte[] data, string cacheName)
    {
        if (this._cachedItems.ContainsKey(playbackItem))
            return true;
        
        var cachePath = Path.Combine(_cacheDirectory, cacheName);
        await File.WriteAllBytesAsync(cachePath, data);
        this._cachedItems.Add(playbackItem, cachePath);
        return true;
    }

    public async ValueTask<bool> CacheAsync(PlaybackItem playbackItem, Stream data, string cacheName)
    {
        if (this._cachedItems.ContainsKey(playbackItem))
            return true;
        
        var cachePath = Path.Combine(_cacheDirectory, cacheName);
        await using var fs = File.Create(cachePath);
        await data.CopyToAsync(fs);
        this._cachedItems.Add(playbackItem, cachePath);
        return true;
    }

    public ValueTask<bool> CacheAsync(PlaybackItem playbackItem, string path, string cacheName)
    {
        var fileName = Path.GetFileName(path);
        if (this._cachedItems.ContainsKey(playbackItem))
            return ValueTask.FromResult(true);
        
        var cachePath = Path.Combine(_cacheDirectory, cacheName);
        File.Copy(path, cachePath, true);
        this._cachedItems.Add(playbackItem, cachePath);
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> RemoveAsync(PlaybackItem playbackItem)
    {
        if (!this._cachedItems.ContainsKey(playbackItem))
            return ValueTask.FromResult(false);
        
        var path = this._cachedItems[playbackItem];
        File.Delete(path);
        this._cachedItems.Remove(playbackItem);
        return ValueTask.FromResult(true);
    }

    public ValueTask<string?> GetCachedPathAsync(PlaybackItem playbackItem)
    {
        if (!this._cachedItems.ContainsKey(playbackItem))
            return ValueTask.FromResult<string?>(null);
        
        return ValueTask.FromResult<string?>(this._cachedItems[playbackItem]);
    }

    public ValueTask<Stream?> GetCachedStreamAsync(PlaybackItem playbackItem)
    {
        if (!this._cachedItems.ContainsKey(playbackItem))
            return ValueTask.FromResult<Stream?>(null);
        
        var path = this._cachedItems[playbackItem];
        return ValueTask.FromResult<Stream?>(File.OpenRead(path));
    }
}