using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces;

public interface IAdvancedAudioBackendService : IAudioBackendService
{
    public ValueTask<IMediaChannel?> CreateChannelAsync(PlayItem playItem, float volume = 1f, int frequency = 44100, AudioDevice? device = null, Action<PlayItem>? onEnded = null);
}