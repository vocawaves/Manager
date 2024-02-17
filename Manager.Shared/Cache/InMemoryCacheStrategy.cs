using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces;
using Manager.Shared.Interfaces.Data;

namespace Manager.Shared.Cache;

public class InMemoryCacheStrategy : ICacheStrategy
{
    private readonly Dictionary<string, MemoryStream> _cache = new();

    public ValueTask<bool> CacheAsync(PlaybackItem playbackItem, byte[] data)
    {
        if (_cache.ContainsKey(playbackItem.OwnerPath))
            return ValueTask.FromResult(true);

        var ms = new MemoryStream(data);
        ms.Seek(0, SeekOrigin.Begin);
        _cache.Add(playbackItem.OwnerPath, ms);
        return ValueTask.FromResult(true);
    }

    public async ValueTask<bool> CacheAsync(PlaybackItem playbackItem, Stream data)
    {
        if (_cache.ContainsKey(playbackItem.OwnerPath))
            return true;

        try
        {
            var ms = new MemoryStream();
            await data.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            _cache.Add(playbackItem.OwnerPath, ms);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    public async ValueTask<bool> CacheAsync(PlaybackItem playbackItem, string path)
    {
        var ms = new MemoryStream();
        await using var fs = File.OpenRead(path);
        await fs.CopyToAsync(ms);
        ms.Seek(0, SeekOrigin.Begin);
        _cache.Add(playbackItem.OwnerPath, ms);
        return true;
    }

    public ValueTask<bool> RemoveAsync(PlaybackItem playbackItem)
    {
        if (!_cache.ContainsKey(playbackItem.OwnerPath))
            return ValueTask.FromResult(false);

        _cache[playbackItem.OwnerPath].Dispose();
        _cache.Remove(playbackItem.OwnerPath);
        playbackItem.CacheState = CacheState.NotCached;
        return ValueTask.FromResult(true);
    }

    public ValueTask<string?> GetCachedPathAsync(PlaybackItem playbackItem)
    {
        return ValueTask.FromResult((string?)playbackItem.OwnerPath);
    }

    public ValueTask<Stream?> GetCachedStreamAsync(PlaybackItem playbackItem)
    {
        if (!_cache.ContainsKey(playbackItem.OwnerPath))
            return ValueTask.FromResult<Stream?>(null);

        return ValueTask.FromResult<Stream?>(_cache[playbackItem.OwnerPath]);
    }
}