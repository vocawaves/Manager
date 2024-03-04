using Manager.Shared.Entities;
using Manager.Shared.Events.Data;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.General;

namespace Manager.Shared.Interfaces.Data;

public interface IDataService : IManagerComponent
{
    #region Events

    //CacheFailed?
    public event AsyncEventHandler<CacheFailedEventArgs>? CacheFailed; 

    #endregion
    
    public ICacheStrategy CacheStrategy { get; }
    
    public ValueTask<bool> CachePlayItemAsync(PlaybackItem item);
    public ValueTask<bool> RemoveFromCacheAsync(PlaybackItem item);
    public ValueTask<PlaybackItem?> GetPlayItemFromUriAsync(string uri);
}