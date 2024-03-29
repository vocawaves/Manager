using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.General;

/// <summary>
/// Defines a service that can create a media channel.
/// </summary>
public interface IBackendService : IManagerComponent
{
    /// <summary>
    /// Creates a media channel for the given media item.
    /// </summary>
    public ValueTask<IMediaChannel?> CreateChannelAsync(MediaItem mediaItem);
}