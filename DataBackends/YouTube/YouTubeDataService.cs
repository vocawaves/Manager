using HeyRed.Mime;
using Manager.Shared;
using Manager.Shared.Cache;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.General;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.Data;
using Manager.Shared.Interfaces.General;
using Microsoft.Extensions.Logging;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Manager.DataBackends.YouTube;

public class YouTubeDataService : ManagerComponent, IStreamingServiceSource, IAudioDataSource, IVideoDataSource
{

    private readonly ILogger<YouTubeDataService>? _logger;
    private readonly ICacheStrategy _cacheStrategy;
    private readonly YoutubeClient _youtubeClient;
    private readonly HttpClient _httpClient;

    private YouTubeDataService(ComponentManager componentManager, string name, ulong parent, IComponentConfiguration? config = null) : base(componentManager, name, parent, config)
    {
        this._logger = componentManager.CreateLogger<YouTubeDataService>();
        if (config is not YouTubeDataServiceConfiguration youtubeConfig || youtubeConfig.CacheStrategy is null)
        {
            this._logger?.LogInformation("No configuration provided for {Name}", name);
            _cacheStrategy = new FolderCacheStrategy(componentManager.CreateLogger<FolderCacheStrategy>());
        }
        else
        {
            this._logger?.LogInformation("Using provided configuration for {Name}", name);
            _cacheStrategy = youtubeConfig.CacheStrategy;
        }
        this._youtubeClient = new YoutubeClient();
        this._httpClient = new HttpClient();
    }

    public override ValueTask<bool> InitializeAsync(params string[] options)
    {
        return ValueTask.FromResult(true);
    }

    public async ValueTask<MediaItem?> GetAudioItemAsync(string uri)
    {
        var videoId = VideoId.TryParse(uri);
        if (videoId is null)
        {
            _logger?.LogError("Failed to parse video id from uri {Uri}", uri);
            return null;
        }

        throw new NotImplementedException();
        //var video = await _youtubeClient.Videos.GetAsync(videoId.Value);
        //this._logger?.LogDebug("Got video {Title} by {Author}", video.Title, video.Author.ChannelTitle);
        //var thumbnailUrl = video.Thumbnails.GetWithHighestResolution();
        //var thumbnailData = await _httpClient.GetByteArrayAsync(thumbnailUrl.Url);
        //var thumbnailMimeType = MimeGuesser.GuessMimeType(thumbnailData);
        //var audioItem = new AudioItem(this, Parent, uri, videoId, video.Title, video.Author.ChannelTitle,
        //    video.Duration ?? TimeSpan.Zero, thumbnailData, thumbnailMimeType,
        //    this._instancer.CreateLogger<AudioItem>());
        //return audioItem;
    }

    public async ValueTask<MediaItem?> GetVideoItemAsync(string uri)
    {
        var videoId = VideoId.TryParse(uri);
        if (videoId is null)
        {
            _logger?.LogError("Failed to parse video id from uri {Uri}", uri);
            return null;
        }
        
        throw new NotImplementedException();
    }

    public ValueTask<MediaItem?> GetSubtitleItemAsync(string uri)
    {
        throw new NotImplementedException();
    }

    public ValueTask<MediaItem[]?> GetAudioItemPlaylistAsync(string uri)
    {
        throw new NotImplementedException();
    }

    public ValueTask<MediaItem[]?> GetVideoItemPlaylistAsync(string uri)
    {
        throw new NotImplementedException();
    }

    public ValueTask<MediaItem[]?> GetSubtitleItemPlaylistAsync(string uri)
    {
        throw new NotImplementedException();
    }

    public ValueTask<MediaItem[]?> GetPlaylistAsync(string url, ItemType type, int limit = 0)
    {
        throw new NotImplementedException();
    }

    public ValueTask<MediaItem[]?> GetAuthorItemsAsync(string url, ItemType itemType, int limit = 0)
    {
        throw new NotImplementedException();
    }

    public ValueTask<MediaItem[]?> SearchAsync(string query, ItemType itemType, SearchFilter filter = SearchFilter.None)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> RemoveMediaItemFromCacheAsync(MediaItem item)
    {
        throw new NotImplementedException();
    }

    public ValueTask<string?> GetCachedMediaItemPathAsync(MediaItem item)
    {
        return this._cacheStrategy.GetCachedPathAsync(item);
    }

    public ValueTask<Stream?> GetCachedMediaItemStreamAsync(MediaItem item)
    {
        return this._cacheStrategy.GetCachedStreamAsync(item);
    }

    public ValueTask<bool> RemoveFromCacheAsync(MediaItem item)
    {
        return this._cacheStrategy.RemoveAsync(item);
    }

    public ValueTask<bool> CacheMediaItemAsync(MediaItem item)
    {
        throw new NotImplementedException();
    }
}