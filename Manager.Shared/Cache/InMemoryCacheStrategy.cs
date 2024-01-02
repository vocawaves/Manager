using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces;

namespace Manager.Shared.Cache;

public class InMemoryCacheStrategy : ICacheStrategy
{
    private Dictionary<string, MemoryStream> _cache = new();

    public ValueTask<bool> CacheAsync(PlayItem playItem, byte[] data)
    {
        if (_cache.ContainsKey(playItem.OwnerPath))
            return ValueTask.FromResult(true);

        var ms = new MemoryStream(data);
        ms.Seek(0, SeekOrigin.Begin);
        _cache.Add(playItem.OwnerPath, ms);
        return ValueTask.FromResult(true);
    }

    public async ValueTask<bool> CacheAsync(PlayItem playItem, Stream data)
    {
        if (_cache.ContainsKey(playItem.OwnerPath))
            return true;

        try
        {
            var ms = new MemoryStream();
            await data.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            _cache.Add(playItem.OwnerPath, ms);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    public async ValueTask<bool> CacheAsync(PlayItem playItem, string path)
    {
        var ms = new MemoryStream();
        await using var fs = File.OpenRead(path);
        await fs.CopyToAsync(ms);
        ms.Seek(0, SeekOrigin.Begin);
        _cache.Add(playItem.OwnerPath, ms);
        return true;
    }

    public ValueTask<bool> RemoveAsync(PlayItem playItem)
    {
        if (!_cache.ContainsKey(playItem.OwnerPath))
            return ValueTask.FromResult(false);

        _cache[playItem.OwnerPath].Dispose();
        _cache.Remove(playItem.OwnerPath);
        playItem.CacheState = CacheState.NotCached;
        return ValueTask.FromResult(true);
    }

    public ValueTask<string?> GetCachedPathAsync(PlayItem playItem)
    {
        return ValueTask.FromResult((string?)playItem.OwnerPath);
    }

    public ValueTask<Stream?> GetCachedStreamAsync(PlayItem playItem)
    {
        if (!_cache.ContainsKey(playItem.OwnerPath))
            return ValueTask.FromResult<Stream?>(null);

        return ValueTask.FromResult<Stream?>(_cache[playItem.OwnerPath]);
    }
}