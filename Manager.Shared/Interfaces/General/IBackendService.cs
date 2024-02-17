using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.General;

public interface IBackendService
{
    public ValueTask<IMediaChannel?> CreateChannelAsync(PlaybackItem playbackItem, Action<PlaybackItem>? onEnded = null);
}