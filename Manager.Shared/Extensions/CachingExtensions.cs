using Manager.Shared.Entities;

namespace Manager.Shared.Extensions;

public static class CachingExtensions
{
    public static ValueTask<bool> CacheAsync(this MediaItem mediaItem)
    {
        return mediaItem.DataService.CacheMediaItemAsync(mediaItem);
    }
    
    public static ValueTask<bool> RemoveAsync(this MediaItem mediaItem)
    {
        return mediaItem.DataService.RemoveMediaItemFromCacheAsync(mediaItem);
    }
    
    public static ValueTask<string?> GetCachedPathAsync(this MediaItem mediaItem)
    {
        return mediaItem.DataService.GetCachedMediaItemPathAsync(mediaItem);
    }
    
    public static ValueTask<Stream?> GetCachedStreamAsync(this MediaItem mediaItem)
    {
        return mediaItem.DataService.GetCachedMediaItemStreamAsync(mediaItem);
    }
}