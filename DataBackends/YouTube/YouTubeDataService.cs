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

namespace Manager.DataBackends.YouTube;

public class YouTubeDataService : IManagerComponent<YouTubeDataServiceConfiguration>, IStreamingServiceSource, IAudioDataSource, IVideoDataSource
{

    private readonly ILogger<YouTubeDataService>? _logger;
    private readonly ICacheStrategy _cacheStrategy;
    private readonly YoutubeClient _youtubeClient;
    private readonly HttpClient _httpClient;

    #region IManagerComponent

    public event AsyncEventHandler? InitSuccess;
    public event AsyncEventHandler<InitFailedEventArgs>? InitFailed;
    public bool Initialized { get; } = true;
    public ComponentManager ComponentManager { get; }
    public string Name { get; }
    public ulong Parent { get; }

    public YouTubeDataServiceConfiguration? Configuration { get; }

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
    
    public YouTubeDataService(ComponentManager componentManager, string name, ulong parent, YouTubeDataServiceConfiguration configuration)
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
    
    public ValueTask<bool> InitializeAsync(params string[] options)
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