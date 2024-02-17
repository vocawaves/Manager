using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces;
using Manager.Shared.Interfaces.Data;

namespace Manager.Shared.Cache;

public class LocalPathCacheStrategy : ICacheStrategy
{
    public ValueTask<bool> CacheAsync(PlaybackItem playbackItem, byte[] data)
    {
        //Since this is a local path cache strategy, we don't need to cache the byte array
        return ValueTask.FromResult(File.Exists(playbackItem.OwnerPath));
    }

    public ValueTask<bool> CacheAsync(PlaybackItem playbackItem, Stream data)
    {
        //Since this is a local path cache strategy, we don't need to cache the stream
        return ValueTask.FromResult(File.Exists(playbackItem.OwnerPath));
    }

    public ValueTask<bool> CacheAsync(PlaybackItem playbackItem, string path)
    {
        if (!File.Exists(path))
            return ValueTask.FromResult(false);

        playbackItem.CacheState = CacheState.LocalPath;
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> RemoveAsync(PlaybackItem playbackItem)
    {
        playbackItem.CacheState = CacheState.NotCached;
        return ValueTask.FromResult(true);
    }

    public ValueTask<string?> GetCachedPathAsync(PlaybackItem playbackItem)
    {
        return ValueTask.FromResult((string?)playbackItem.OwnerPath);
    }

    public ValueTask<Stream?> GetCachedStreamAsync(PlaybackItem playbackItem)
    {
        if (playbackItem.CacheState != CacheState.LocalPath)
            return ValueTask.FromResult<Stream?>(null);
        return ValueTask.FromResult((Stream?)File.OpenRead(playbackItem.OwnerPath));
    }
}