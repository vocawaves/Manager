using Manager.Shared.Entities;
using Manager.Shared.Interfaces.General;

namespace Manager.Shared.Interfaces.Data;

/// <summary>
/// This service works in combination with the IAudioDataService, IVideoDataService and others to get MediaItems in a standardised way.
/// </summary>
public interface IDataService : IManagerComponent
{
    /// <summary>
    /// Cache the media item.
    /// </summary>
    public ValueTask<bool> CachePlayItemAsync(MediaItem item);
    /// <summary>
    /// Remove the media item from the cache.
    /// </summary>
    public ValueTask<bool> RemoveFromCacheAsync(MediaItem item);
    
    /// <summary>
    /// Get the cached path of the media item.
    /// </summary>
    public ValueTask<string?> GetCachedMediaItemPathAsync(MediaItem item);
    /// <summary>
    /// Get the cached stream of the media item.
    /// </summary>
    public ValueTask<Stream?> GetCachedMediaItemStreamAsync(MediaItem item);
}