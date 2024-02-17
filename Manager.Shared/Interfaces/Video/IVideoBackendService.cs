using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.General;

namespace Manager.Shared.Interfaces.Video;

public interface IVideoBackendService : IBackendService
{
    public event AsyncEventHandler? GlobalExternalPlayerSurfaceAdded;
    public event AsyncEventHandler? GlobalExternalPlayerSurfaceRemoved;

    public List<IExternalPlayerSurface> ExternalPlayerSurfaces { get; }
    public ValueTask<IExternalPlayerSurface> CreateExternalPlayerSurfaceAsync(string name);
    public ValueTask<bool> DestroyExternalPlayerSurfaceAsync(IExternalPlayerSurface externalPlayerSurface);
}