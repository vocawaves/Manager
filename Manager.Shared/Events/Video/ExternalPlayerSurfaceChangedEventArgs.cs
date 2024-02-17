using Manager.Shared.Interfaces.Video;

namespace Manager.Shared.Events.Video;

public class ExternalPlayerSurfaceChangedEventArgs : EventArgs
{
    public IExternalPlayerSurface NewSurface { get; }
    
    public ExternalPlayerSurfaceChangedEventArgs(IExternalPlayerSurface newSurface)
    {
        this.NewSurface = newSurface;
    }
}