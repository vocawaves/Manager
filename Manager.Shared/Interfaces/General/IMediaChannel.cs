using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.General;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.General;

public interface IMediaChannel : IAsyncDisposable
{
    #region Events

    public event AsyncEventHandler? Playing;

    public event AsyncEventHandler? Paused;

    public event AsyncEventHandler? Resumed;

    public event AsyncEventHandler? Stopped;
    
    public event AsyncEventHandler<ChannelStateChangedEventArgs>? StateChanged;

    public event AsyncEventHandler<ChannelPositionChangedEventArgs>? PositionChanged; 
    //StateChanged
    //Playing
    //Paused
    //Resumed
    //Stopped
    
    //PositionChanged(?) -> Only from SetPosition?
    //                   -> Timer to update Position?
    //                   -> Time probably better (global update/interval time?)

    #endregion
    
    public IBackendService AssociatedBackend { get; }
    public PlaybackItem PlaybackItem { get; }

    public ValueTask<bool> PlayAsync();
    public ValueTask<bool> PauseAsync();
    public ValueTask<bool> ResumeAsync();
    public ValueTask<bool> StopAsync();
    
    public ValueTask<ChannelState?> GetStateAsync();
    public ValueTask<bool> SetStateAsync(ChannelState state);
    
    public ValueTask<TimeSpan?> GetPositionAsync();
    public ValueTask<bool> SetPositionAsync(double positionMs);
    public ValueTask<bool> SetPositionAsync(TimeSpan position);
    
    public ValueTask<TimeSpan?> GetLengthAsync();
    
    public ValueTask<bool> DestroyAsync();
}