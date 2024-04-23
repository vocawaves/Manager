using Manager.Shared.Events.Video;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.Video;

/// <summary>
/// A channel that can handle video.
/// </summary>
public interface IVideoChannel
{
    #region Events

    /// <summary>
    /// Fired when the video surface of this channel changes.
    /// </summary>
    public event AsyncEventHandler<ExternalPlayerSurfaceChangedEventArgs>? ExternalVideoSurfaceChanged;

    #endregion
    
    public IExternalPlayerSurface? ExternalVideoSurface { get; }
    
    /// <summary>
    /// Sets the external video surface of this channel.
    /// Can be obtained from its backend service.
    /// Also fires the <see cref="ExternalVideoSurfaceChanged"/> event.
    /// </summary>
    public ValueTask<bool> SetExternalVideoSurfaceAsync(IExternalPlayerSurface surface);
}