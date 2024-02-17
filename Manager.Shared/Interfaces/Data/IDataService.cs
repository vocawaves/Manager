using Manager.Shared.Entities;
using Manager.Shared.Events.Data;
using Manager.Shared.Events.General;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.Data;

public interface IDataService
{
    #region Events

    //CacheFailed?
    public event AsyncEventHandler<CacheFailedEventArgs>? CacheFailed; 

    #endregion
    
    public ValueTask<bool> CachePlayItemAsync(PlaybackItem item);
    public ValueTask<bool> RemoveFromCacheAsync(PlaybackItem item);
    public ValueTask<PlaybackItem?> GetPlayItemAsync(FileItem item);
}