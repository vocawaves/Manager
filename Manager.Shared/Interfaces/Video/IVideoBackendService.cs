using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.General;

namespace Manager.Shared.Interfaces.Video;

/// <summary>
/// Defines a service that can handle video.
/// It should handle its video player surfaces.
/// </summary>
public interface IVideoBackendService : IBackendService
{
    /// <summary>
    /// Fired when a new external player surface is added.
    /// </summary>
    public event AsyncEventHandler? GlobalExternalPlayerSurfaceAdded;
    /// <summary>
    /// Fired when an external player surface is removed.
    /// </summary>
    public event AsyncEventHandler? GlobalExternalPlayerSurfaceRemoved;

    /// <summary>
    /// A list of all external player surfaces. Should not be modified from the outside.
    /// </summary>
    public List<IExternalPlayerSurface> ExternalPlayerSurfaces { get; }
    /// <summary>
    /// Creates a new external player surface.
    /// Also fires the <see cref="GlobalExternalPlayerSurfaceAdded"/> event.
    /// </summary>
    public ValueTask<IExternalPlayerSurface> CreateExternalPlayerSurfaceAsync(string name);
    /// <summary>
    /// Removes an external player surface.
    /// Also fires the <see cref="GlobalExternalPlayerSurfaceRemoved"/> event.
    /// </summary>
    public ValueTask<bool> DestroyExternalPlayerSurfaceAsync(IExternalPlayerSurface externalPlayerSurface);
}