using Manager.Shared.Interfaces.Data;

namespace Manager.Shared.Entities;

public class MediaItem
{
    public string SourcePath { get; }
    public ulong OwnerId { get; }
    public string PathTitle { get; }
    public string? CustomTitle { get; set; }
    public string DisplayTitle => CustomTitle ?? PathTitle;
    
    
    public string? MimeType { get; set; }

    public bool IsCached { get; internal set; }
    
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    internal IDataService DataService { get; set; }
    
    public MediaItem(IDataService dataService, ulong ownerId, string sourcePath, string pathTitle)
    {
        DataService = dataService;
        OwnerId = ownerId;
        SourcePath = sourcePath;
        PathTitle = pathTitle;
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
}