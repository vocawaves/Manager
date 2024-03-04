using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.General;

public interface IBackendService : IManagerComponent
{
    public ValueTask<IMediaChannel?> CreateChannelAsync(PlaybackItem playbackItem);
}