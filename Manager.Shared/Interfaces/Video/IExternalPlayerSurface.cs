namespace Manager.Shared.Interfaces.Video;

/// <summary>
/// A wrapper for a video player surface that is not managed by the manager.
/// </summary>
public interface IExternalPlayerSurface
{
    
    /// <summary>
    /// A descriptive name for the surface.
    /// Can also be just a part of the screen if the surface is split.
    /// </summary>
    public string SurfaceName { get; }

    /// <summary>
    /// The current target of the surface. Window handle or other target. a bit WIP
    /// </summary>
    public IntPtr? GetNativeReference();

    public ValueTask<bool> PrepareVideoAsync(IVideoChannel channel);
}