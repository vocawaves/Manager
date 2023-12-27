using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces;

public interface IDataService
{
    public ValueTask<string[]> GetDirectoriesAsync(string? path = null);

    public ValueTask<string[]> GetFilesAsync(string? path = null, params string[] extensions);

    public ValueTask<PlayItem?> GetPlayItemAsync(string path);
    
    public ValueTask<PlayItem?> CachePlayItemAsync(PlayItem item);
    
    public ValueTask<bool> RemovePlayItemFromCacheAsync(string path);
    public ValueTask<bool> RemovePlayItemFromCacheAsync(PlayItem item);
    
    
}