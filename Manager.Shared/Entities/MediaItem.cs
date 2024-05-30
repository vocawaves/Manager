using Manager.Shared.Enums;
using Manager.Shared.Events.Data;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;

namespace Manager.Shared.Entities;

/// <summary>
/// A standardized wrapper for media items.
/// </summary>
public class MediaItem
{
    /// <summary>
    /// Fired when the cache state of the media item changes.
    /// </summary>
    public event AsyncEventHandler<CacheStateChangedEventArgs>? CacheStateChanged;
    /// <summary>
    /// Fired when the cache progress of the media item changes.
    /// For example, when the item is being cached. (0-100%)
    /// </summary>
    public event AsyncEventHandler<CacheProgressChangesEventArgs>? CacheProgressChanged;
    
    /// <summary>
    /// Fired when the media item is cached.
    /// </summary>
    public event AsyncEventHandler? Cached;
    /// <summary>
    /// Fired when the media item is removed from cache.
    /// </summary>
    public event AsyncEventHandler? RemovedFromCache;
    /// <summary>
    /// Fired when the media item fails to cache.
    /// </summary>
    public event AsyncEventHandler? CacheFailed;
    
    /// <summary>
    /// Type of the media item.
    /// </summary>
    public ItemType ItemType { get; }
    
    /// <summary>
    /// Source path of the media item. Can be a URL or a file path.
    /// </summary>
    public string SourcePath { get; }
    
    /// <summary>
    /// Parent ID of the media item. Unused for now (always 0)
    /// For later remote management.
    /// </summary>
    public ulong ParentId { get; }
    
    /// <summary>
    /// Basic title of the media item. Usually the file name YouTube videoID for example.
    /// </summary>
    public string PathTitle { get; }

    /// <summary>
    /// Cache state of the media item.
    /// </summary>
    public CacheState CacheState { get; private set; } = CacheState.NotCached;
    
    /// <summary>
    /// Data service this media item is associated with.
    /// </summary>
    public IDataService DataService { get; }
    
    private readonly ILogger<MediaItem>? _logger;
    
    public MediaItem(IDataService dataService, ItemType itemType, ulong parentId, string sourcePath, string pathTitle, ILogger<MediaItem>? logger = null)
    {
        _logger = logger;
        ItemType = itemType;
        DataService = dataService;
        ParentId = parentId;
        SourcePath = sourcePath;
        PathTitle = pathTitle;
    }
    
    public void SetCacheState(CacheState state)
    {
        CacheState = state;
        CacheStateChanged?.InvokeAndForget(this, new CacheStateChangedEventArgs(state));
        _logger?.LogDebug("Cache state of {PathTitle} changed to {CacheState}", PathTitle, state);
    }
    
    public void SetCacheProgress(int progress)
    {
        CacheProgressChanged?.InvokeAndForget(this, new CacheProgressChangesEventArgs(progress));
        _logger?.LogDebug("Cache progress of {PathTitle} changed to {Progress}%", PathTitle, progress);
    }
}