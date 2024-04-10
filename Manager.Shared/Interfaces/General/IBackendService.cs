using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.General;

/// <summary>
/// Defines a service that can create a media channel.
/// </summary>
public interface IBackendService : IManagerComponent
{
    /// <summary>
    /// Checks if the media item is supported by the backend.
    /// </summary>
    public ValueTask<bool> IsMediaItemSupportedAsync(MediaItem mediaItem);
}