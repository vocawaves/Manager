using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.General;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.General;

/// <summary>
/// Base interface for all types of media channels.
/// </summary>
public interface IMediaChannel : IAsyncDisposable
{
    #region Events

    /// <summary>
    /// Fired when the channel is playing.
    /// </summary>
    public event AsyncEventHandler? Playing;

    /// <summary>
    /// Fired when the channel is paused.
    /// </summary>
    public event AsyncEventHandler? Paused;

    /// <summary>
    /// Fired when the channel is resumed.
    /// </summary>
    public event AsyncEventHandler? Resumed;

    /// <summary>
    /// Fired when the channel is stopped.
    /// </summary>
    public event AsyncEventHandler? Stopped;
    
    /// <summary>
    /// Fired when the channel has ended.
    /// Basically when the end of the media item is reached (Position == Length)
    /// </summary>
    public event AsyncEventHandler? Ended;
    
    /// <summary>
    /// Fired when the channel state has changed.
    /// A combination of the Playing, Paused, Resumed and Stopped events.
    /// </summary>
    public event AsyncEventHandler<ChannelStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Fired when the channel position has changed.
    /// </summary>
    public event AsyncEventHandler<ChannelPositionChangedEventArgs>? PositionChanged; 
    
    /// <summary>
    /// Fired when a position trigger is invoked.
    /// </summary>
    public event AsyncEventHandler<ChannelPositionTriggerInvokedEventArgs> PositionTriggerInvoked;

    #endregion
    
    /// <summary>
    /// The backend service that created this channel.
    /// </summary>
    public IBackendService AssociatedBackend { get; }
    /// <summary>
    /// This channel's media item.
    /// </summary>
    public MediaItem MediaItem { get; }
    
    /// <summary>
    /// A list of position triggers. Should not be modified from the outside.
    /// </summary>
    public List<PositionTrigger> PositionTriggers { get; }

    /// <summary>
    /// Start playing the media item.
    /// If the media item is already playing or paused, it should restart from the beginning.
    /// Also should fire the Playing event and the StateChanged event.
    /// </summary>
    public ValueTask<bool> PlayAsync();
    /// <summary>
    /// Pause the media item.
    /// Also should fire the Paused event and the StateChanged event.
    /// </summary>
    public ValueTask<bool> PauseAsync();
    /// <summary>
    /// Resume the media item.
    /// Also should fire the Resumed event and the StateChanged event.
    /// </summary>
    public ValueTask<bool> ResumeAsync();
    /// <summary>
    /// Stop the media item.
    /// Sets the position to the beginning.
    /// Also should fire the Stopped event and the StateChanged event.
    /// </summary>
    public ValueTask<bool> StopAsync();
    
    /// <summary>
    /// Gets the current state of the channel.
    /// </summary>
    public ValueTask<ChannelState> GetStateAsync();
    /// <summary>
    /// Sets the state of the channel.
    /// Should just redirect to the Play, Pause, Resume and Stop methods.
    /// </summary>
    public ValueTask<bool> SetStateAsync(ChannelState state);
    
    /// <summary>
    /// Gets the current position of the channel.
    /// </summary>
    public ValueTask<TimeSpan?> GetPositionAsync();
    /// <summary>
    /// Sets the position of the channel in milliseconds.
    /// </summary>
    public ValueTask<bool> SetPositionAsync(double positionMs);
    /// <summary>
    /// Sets the position of the channel via a TimeSpan.
    /// </summary>
    public ValueTask<bool> SetPositionAsync(TimeSpan position);
    
    /// <summary>
    /// Adds a position trigger to the channel.
    /// The name should be unique to identify the trigger when the event is fired.
    /// </summary>
    public ValueTask<PositionTrigger?> AddPositionTriggerAsync(string name, TimeSpan position);
    /// <summary>
    /// Removes a position trigger from the channel.
    /// </summary>
    public ValueTask<bool> RemovePositionTriggerAsync(PositionTrigger trigger);
    
    /// <summary>
    /// Gets the (real) length of the media item.
    /// </summary>
    public ValueTask<TimeSpan?> GetLengthAsync();
    
    /// <summary>
    /// Destroys the channel and releases all resources.
    /// Should stop the media item if it's playing too.
    /// </summary>
    public ValueTask<bool> DestroyAsync();
}