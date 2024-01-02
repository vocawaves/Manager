using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces;

namespace Manager.Shared.Cache;

public class LocalPathCacheStrategy : ICacheStrategy
{
    public ValueTask<bool> CacheAsync(PlayItem playItem, byte[] data)
    {
        //Since this is a local path cache strategy, we don't need to cache the byte array
        return ValueTask.FromResult(File.Exists(playItem.OwnerPath));
    }

    public ValueTask<bool> CacheAsync(PlayItem playItem, Stream data)
    {
        //Since this is a local path cache strategy, we don't need to cache the stream
        return ValueTask.FromResult(File.Exists(playItem.OwnerPath));
    }

    public ValueTask<bool> CacheAsync(PlayItem playItem, string path)
    {
        if (!File.Exists(path))
            return ValueTask.FromResult(false);

        playItem.CacheState = CacheState.LocalPath;
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> RemoveAsync(PlayItem playItem)
    {
        playItem.CacheState = CacheState.NotCached;
        return ValueTask.FromResult(true);
    }

    public ValueTask<string?> GetCachedPathAsync(PlayItem playItem)
    {
        return ValueTask.FromResult((string?)playItem.OwnerPath);
    }

    public ValueTask<Stream?> GetCachedStreamAsync(PlayItem playItem)
    {
        if (playItem.CacheState != CacheState.LocalPath)
            return ValueTask.FromResult<Stream?>(null);
        return ValueTask.FromResult((Stream?)File.OpenRead(playItem.OwnerPath));
    }
}