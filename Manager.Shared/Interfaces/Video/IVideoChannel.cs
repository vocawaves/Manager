using Manager.Shared.Events.Video;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.Video;

public interface IVideoChannel
{
    #region Events

    public event AsyncEventHandler<ExternalPlayerSurfaceChangedEventArgs>? ExternalVideoSurfaceChanged;

    #endregion
    
    public ValueTask<bool> SetExternalVideoSurfaceAsync(IExternalPlayerSurface? surface);
    public ValueTask<IExternalPlayerSurface?> GetExternalVideoSurfaceAsync();
}