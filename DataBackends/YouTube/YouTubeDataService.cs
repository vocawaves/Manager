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
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Manager.DataBackends.YouTube;

public class YouTubeDataService : IManagerComponent<YouTubeDataServiceConfiguration>, IStreamingServiceSource,
    IAudioDataSource, IVideoDataSource
{
    private readonly ILogger<YouTubeDataService>? _logger;
    private readonly ICacheStrategy _cacheStrategy;
    private readonly YoutubeClient _youtubeClient;
    private readonly HttpClient _httpClient;

    #region IManagerComponent

    public ComponentManager ComponentManager { get; }
    public string Name { get; }
    public ulong Parent { get; }

    public YouTubeDataServiceConfiguration Configuration { get; }

    #endregion

    public YouTubeDataService(ComponentManager componentManager, string name, ulong parent)
    {
        _logger = componentManager.CreateLogger<YouTubeDataService>();
        ComponentManager = componentManager;
        Name = name;
        Parent = parent;
        _youtubeClient = new YoutubeClient();
        _httpClient = new HttpClient();
        var cacheStrategy = componentManager.CreateComponent<FolderCacheStrategy, FolderCacheStrategyConfiguration>(
            "YouTubeCacheStrategy", parent, new FolderCacheStrategyConfiguration
            {
                CacheFolder = Path.Combine(Directory.GetCurrentDirectory(), "YouTubeCache")
            });
        if (cacheStrategy is null)
        {
            _logger?.LogError("Failed to create cache strategy");
            throw new InvalidOperationException("Failed to create cache strategy");
        }

        Configuration = new YouTubeDataServiceConfiguration
        {
            CacheStrategy = cacheStrategy
        };
        _cacheStrategy = cacheStrategy;
    }

    public YouTubeDataService(ComponentManager componentManager, string name, ulong parent,
        YouTubeDataServiceConfiguration configuration)
    {
        _youtubeClient = new YoutubeClient();
        _httpClient = new HttpClient();
        _logger = componentManager.CreateLogger<YouTubeDataService>();
        ComponentManager = componentManager;
        Name = name;
        Parent = parent;
        Configuration = configuration;
        if (configuration.CacheStrategy is not null)
        {
            _cacheStrategy = configuration.CacheStrategy;
        }
        else
        {
            var cacheStrategy = componentManager.CreateComponent<FolderCacheStrategy, FolderCacheStrategyConfiguration>(
                "YouTubeCacheStrategy", parent, new FolderCacheStrategyConfiguration
                {
                    CacheFolder = Path.Combine(Directory.GetCurrentDirectory(), "YouTubeCache")
                });
            if (cacheStrategy is null)
            {
                _logger?.LogError("Failed to create cache strategy");
                throw new InvalidOperationException("Failed to create cache strategy");
            }

            Configuration.CacheStrategy = cacheStrategy;
            _cacheStrategy = cacheStrategy;
        }
    }

    public async ValueTask<MediaItem?> GetAudioItemAsync(string uri)
    {
        var videoId = VideoId.TryParse(uri);
        if (videoId is null)
        {
            _logger?.LogError("Failed to parse video id from uri {Uri}", uri);
            return null;
        }

        try
        {
            var video = await _youtubeClient.Videos.GetAsync(videoId.Value);
            var logger = ComponentManager.CreateLogger<MediaItem>();
            var mediaItem = new MediaItem(this, ItemType.Audio, this.Parent, uri, video.Id, logger);
            return mediaItem;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to get video manifest for video id {VideoId}", videoId);
            return null;
        }
    }

    public async ValueTask<MediaItem?> GetVideoItemAsync(string uri)
    {
        var videoId = VideoId.TryParse(uri);
        if (videoId is null)
        {
            _logger?.LogError("Failed to parse video id from uri {Uri}", uri);
            return null;
        }

        try
        {
            var video = await _youtubeClient.Videos.GetAsync(videoId.Value);
            var logger = ComponentManager.CreateLogger<MediaItem>();
            var mediaItem = new MediaItem(this, ItemType.Video, this.Parent, uri, video.Id, logger);
            return mediaItem;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to get video manifest for video id {VideoId}", videoId);
            return null;
        }
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

    public async ValueTask<bool> CacheMediaItemAsync(MediaItem item)
    {
        if (item.CacheState is CacheState.Cached or CacheState.Downloading or CacheState.DiskCaching)
        {
            _logger?.LogDebug("Media item {PathTitle} is already cached or being cached", item.PathTitle);
            return item.CacheState is CacheState.Cached;
        }

        var videoId = VideoId.TryParse(item.SourcePath);
        if (videoId is null)
        {
            _logger?.LogError("Failed to parse video id from uri {Uri}", item.SourcePath);
            return true;
        }

        var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId.Value);
        var streamInfo = item.ItemType switch
        {
            ItemType.Audio => streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate(),
            ItemType.Video => streamManifest.GetVideoStreams().GetWithHighestVideoQuality(),
            _ => null
        };

        if (streamInfo is null)
        {
            _logger?.LogError("Failed to get stream info for video id {VideoId}", videoId);
            return false;
        }

        var outputStream = new MemoryStream();
        await _youtubeClient.Videos.Streams.CopyToAsync(streamInfo, outputStream,
            new Progress<double>((d) => CacheProgressUpdated(d, item)));
        
        var cacheNameTypePrefix = item.ItemType switch
        {
            ItemType.Audio => "MA_",
            ItemType.Video => "MV_",
            _ => "XX_"
        };
        var cacheName = $"{cacheNameTypePrefix}{videoId}.mcb"; //manager cache buffer
        var couldCache = await this._cacheStrategy.CacheAsync(item, outputStream, cacheName);
        return couldCache;
    }

    private void CacheProgressUpdated(double progress, MediaItem mediaItem)
    {
        mediaItem.SetCacheProgress((int)progress);
    }
}