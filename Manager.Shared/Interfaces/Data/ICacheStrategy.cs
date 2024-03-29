using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.Data;

/// <summary>
/// Handles caching of media items to help with flash drives and slow internet connections.
/// All MediaItems need to be cached before they can be played.
/// </summary>
public interface ICacheStrategy
{
    /// <summary>
    /// Cache the media item from a byte array.
    /// </summary>
    public ValueTask<bool> CacheAsync(MediaItem mediaItem, byte[] data, string cacheName);
    /// <summary>
    /// Cache the media item from a stream.
    /// </summary>
    public ValueTask<bool> CacheAsync(MediaItem mediaItem, Stream data, string cacheName);
    /// <summary>
    /// Cache the media item from a file path.
    /// </summary>
    public ValueTask<bool> CacheAsync(MediaItem mediaItem, string path, string cacheName);

    /// <summary>
    /// Remove/un-cache the media item.
    /// </summary>
    public ValueTask<bool> RemoveAsync(MediaItem mediaItem);

    /// <summary>
    /// Get the cached path of the media item.
    /// </summary>
    public ValueTask<string?> GetCachedPathAsync(MediaItem mediaItem);
    /// <summary>
    /// Get the cached byte array of the media item.
    /// </summary>
    public ValueTask<Stream?> GetCachedStreamAsync(MediaItem mediaItem);
    
    //TODO: Get a cached MediaItem as a byte array.
    //TODO: Add a method to clear the cache.
}