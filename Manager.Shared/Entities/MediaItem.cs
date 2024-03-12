using Manager.Shared.Enums;
using Manager.Shared.Events.Data;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;

namespace Manager.Shared.Entities;

public class MediaItem : IAsyncDisposable
{
    public event AsyncEventHandler<CacheStateChangedEventArgs>? CacheStateChanged;
    public event AsyncEventHandler<CacheProgressChangesEventArgs>? CacheProgressChanged;
    
    public event AsyncEventHandler? Cached;
    public event AsyncEventHandler? RemovedFromCache;
    public event AsyncEventHandler? CacheFailed;
    
    public string SourcePath { get; }
    public ulong OwnerId { get; }
    public string PathTitle { get; }
    public string? CustomTitle { get; set; }
    public string DisplayTitle => CustomTitle ?? PathTitle;
    
    
    public string? MimeType { get; set; }

    public CacheState CacheState { get; private set; } = CacheState.NotCached;
    public double CacheProgress { get; private set; } = 0;
    
    public Dictionary<string, string> Metadata { get; set; } = new();
    
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
    
    public ValueTask<bool> CacheAsync()
    {
        return this.DataService.CachePlayItemAsync(this);
    }

    public ValueTask<string?> GetCachePathAsync()
    {
        return this.DataService.GetCachedMediaItemPathAsync(this);
    }

    public ValueTask<Stream?> GetCacheStreamAsync()
    {
        return this.DataService.GetCachedMediaItemStreamAsync(this);
    }
    
    public ValueTask<bool> RemoveFromCacheAsync()
    {
        return this.DataService.RemoveFromCacheAsync(this);
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (this.CacheState == CacheState.Caching 
            || this.CacheState == CacheState.Cached)
            await this.RemoveFromCacheAsync();
    }
}