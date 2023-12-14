using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces;

public interface IDataService
{
    public ValueTask<string[]> GetDirectoriesAsync(string? path = null);

    public ValueTask<string[]> GetFilesAsync(string? path = null, params string[] extensions);

    public ValueTask<PlayItem?> GetPlayItemAsync(string path);
    
    public ValueTask<bool> CachePlayItemAsync(PlayItem item);
}