using Manager.Shared.Events.Video;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.Video;

/// <summary>
/// A wrapper for a video player surface that is not managed by the manager.
/// </summary>
public interface IExternalPlayerSurface
{
    /// <summary>
    /// A descriptive name for the screen.
    /// Can also be just a part of the screen if the screen is split.
    /// </summary>
    public string ScreenName { get; }

    /// <summary>
    /// The current target of the surface. Window handle or other target. a bit WIP
    /// </summary>
    public IntPtr CurrentTarget { get; }
    
    /// <summary>
    /// Fired when the target of the surface changes.
    /// </summary>
    public event AsyncEventHandler<ExternalPlayerSurfaceTargetChanged>? TargetChanged;
    
    /// <summary>
    /// Sets the target of the surface.
    /// Also fires the <see cref="TargetChanged"/> event.
    /// </summary>
    public ValueTask<bool> SetTargetAsync(IntPtr target);
}