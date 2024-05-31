using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.General;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;

namespace Manager.Shared.Cache;

public class InMemoryCacheStrategy : ICacheStrategy
{
    #region IManagerComponent

    public event AsyncEventHandler? InitSuccess;
    public event AsyncEventHandler<InitFailedEventArgs>? InitFailed;
    public bool Initialized { get; } = true;
    public ComponentManager ComponentManager { get; }
    public string Name { get; }
    public ulong Parent { get; }

    #endregion
    
    private readonly Dictionary<MediaItem, byte[]> _cachedItems = new();

    private readonly ILogger<InMemoryCacheStrategy>? _logger;

    public InMemoryCacheStrategy(ComponentManager componentManager, string name, ulong parent)
    {
        ComponentManager = componentManager;
        Name = name;
        Parent = parent;
        _logger = componentManager.CreateLogger<InMemoryCacheStrategy>();
    }
    
    public ValueTask<bool> InitializeAsync(params string[] options)
    {
        return ValueTask.FromResult(true);
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