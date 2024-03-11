using Manager.Shared.Entities;
using Manager.Shared.Interfaces.General;

namespace Manager.Shared.Interfaces.Data;

public interface IDataService : IManagerComponent
{
    public ValueTask<bool> CachePlayItemAsync(MediaItem item);
    public ValueTask<bool> RemoveFromCacheAsync(MediaItem item);
    
    public ValueTask<string?> GetCachedMediaItemPathAsync(MediaItem item);
    public ValueTask<Stream?> GetCachedMediaItemStreamAsync(MediaItem item);
}