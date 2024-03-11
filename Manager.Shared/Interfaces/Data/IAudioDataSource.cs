using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.Data;

public interface IAudioDataSource
{
    public ValueTask<AudioItem?> GetAudioItemAsync(string uri);
    public ValueTask<AudioItem[]?> GetAudioItemPlaylistAsync(string uri);
}