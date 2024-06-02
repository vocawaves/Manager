namespace Manager.Shared.Interfaces.Video;

/// <summary>
/// A wrapper for a video player surface that is not managed by the manager.
/// </summary>
public interface IExternalPlayerSurface
{
    public ValueTask<bool> SetPlayerToControlAsync(object playerObj);
}