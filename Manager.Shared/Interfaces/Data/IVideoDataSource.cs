using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.Data;

public interface IVideoDataSource
{
    public ValueTask<VideoItem?> GetVideoItemAsync(string uri);
    public ValueTask<VideoItem[]?> GetVideoItemPlaylistAsync(string uri);
}