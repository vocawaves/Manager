using Manager.Shared.Enums;
using Manager.Shared.Events.Data;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;

namespace Manager.Shared.Entities;

/// <summary>
/// A standardized wrapper for media items.
/// </summary>
public class MediaItem : IAsyncDisposable
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
    /// Source path of the media item. Can be a URL or a file path.
    /// </summary>
    public string SourcePath { get; }
    /// <summary>
    /// Owner ID of the media item. Unused for now (always 0)
    /// For later remote management.
    /// </summary>
    public ulong OwnerId { get; }
    /// <summary>
    /// Basic title of the media item. Usually the file name YouTube videoID for example.
    /// </summary>
    public string PathTitle { get; }
    /// <summary>
    /// Custom title of the media item. Can be set by the user.
    /// </summary>
    public string? CustomTitle { get; set; }
    /// <summary>
    /// Display title of the media item. Custom title if available, otherwise path title.
    /// Might be used in UI. Should maybe be moved to a view model.
    /// </summary>
    public virtual string DisplayTitle => CustomTitle ?? PathTitle;
    
    /// <summary>
    /// Mime type of the media item.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Cache state of the media item.
    /// </summary>
    public CacheState CacheState { get; private set; } = CacheState.NotCached;
    /// <summary>
    /// Cache progress of the media item. (0-100%)
    /// </summary>
    public double CacheProgress { get; private set; } = 0;
    
    /// <summary>
    /// All metadata of the media item.
    /// (may include duplicates)
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    /// <summary>
    /// Data service this media item is associated with.
    /// </summary>
    internal IDataService DataService { get; set; }
    
    private readonly ILogger<MediaItem> _logger;
    
    public MediaItem(ILoggerFactory lf, IDataService dataService, ulong ownerId, string sourcePath, string pathTitle)
    {
        _logger = lf.CreateLogger<MediaItem>();
        DataService = dataService;
        OwnerId = ownerId;
        SourcePath = sourcePath;
        PathTitle = pathTitle;
    }
    
    /// <summary>
    /// Sets the cache state of the media item.
    /// Should be used by the data service/cache strategy.
    /// </summary>
    public void SetCacheState(CacheState state)
    {
        if (this.CacheState == state)
        {
            this._logger.LogDebug("Cache state is already {State}", state);
            return;
        }
        this._logger.LogInformation("Cache state changed to {State}", state);
        this.CacheState = state;
        switch (state)
        {
            case CacheState.Cached:
                this.Cached?.InvokeAndForget(this, EventArgs.Empty);
                break;
            case CacheState.NotCached:
                this.RemovedFromCache?.InvokeAndForget(this, EventArgs.Empty);
                break;
            case CacheState.Failed:
                this.CacheFailed?.InvokeAndForget(this, EventArgs.Empty);
                break;
        }
        this.CacheStateChanged?.InvokeAndForget(this, new CacheStateChangedEventArgs(state));
    }
    
    /// <summary>
    /// Sets the cache progress of the media item.
    /// Should be used by the data service/cache strategy.
    /// </summary>
    public void SetCacheProgress(double progress)
    {
        if (this.CacheProgress.Equals(progress))
        {
            this._logger.LogDebug("Cache progress is already {Progress}", progress);
            return;
        }
        
        this._logger.LogInformation("Cache progress changed to {Progress}", progress);
        this.CacheProgress = progress;
        this.CacheProgressChanged?.InvokeAndForget(this, new CacheProgressChangesEventArgs(progress));
    }
    
    /// <summary>
    /// Caches the media item.
    /// </summary>
    public ValueTask<bool> CacheAsync()
    {
        return this.DataService.CachePlayItemAsync(this);
    }

    /// <summary>
    /// Gets the cache path of the media item.
    /// </summary>
    public ValueTask<string?> GetCachePathAsync()
    {
        return this.DataService.GetCachedMediaItemPathAsync(this);
    }

    /// <summary>
    /// Gets the cache stream of the media item.
    /// </summary>
    public ValueTask<Stream?> GetCacheStreamAsync()
    {
        return this.DataService.GetCachedMediaItemStreamAsync(this);
    }
    
    /// <summary>
    /// Removes the media item from cache.
    /// </summary>
    public ValueTask<bool> RemoveFromCacheAsync()
    {
        return this.DataService.RemoveFromCacheAsync(this);
    }

    /// <summary>
    /// Disposes the media item.
    /// Additionally removes it from cache if it is cached.
    /// </summary>
    public virtual async ValueTask DisposeAsync()
    {
        if (this.CacheState == CacheState.Caching 
            || this.CacheState == CacheState.Cached)
            await this.RemoveFromCacheAsync();
    }
}