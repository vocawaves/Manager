using HeyRed.Mime;
using Manager.Shared.Cache;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.General;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Manager.YouTubeDataService;

public class YouTubeDataService : IStreamingServiceSource, IAudioDataSource, IVideoDataSource
{
    public event AsyncEventHandler<InitSuccessEventArgs>? InitSuccess;
    public event AsyncEventHandler<InitFailedEventArgs>? InitFailed;

    public bool Initialized { get; }
    public string Name { get; }
    public ulong Parent { get; }

    private readonly ILogger<YouTubeDataService> _logger;
    private readonly ICacheStrategy _cacheStrategy;
    private readonly YoutubeClient _youtubeClient;
    private readonly HttpClient _httpClient;

    public YouTubeDataService(ILoggerFactory lf, string name, ulong parent)
    {
        _cacheStrategy = new BasicCacheStrategy(lf);
        _logger = lf.CreateLogger<YouTubeDataService>();
        Name = name;
        Parent = parent;
        _youtubeClient = new YoutubeClient();
        _httpClient = new HttpClient();
    }


    public async ValueTask<AudioItem?> GetAudioItemAsync(string uri)
    {
        var videoId = VideoId.TryParse(uri);
        if (videoId is null)
        {
            _logger.LogError("Failed to parse video id from uri {Uri}", uri);
            return null;
        }

        var video = await _youtubeClient.Videos.GetAsync(videoId.Value);
        this._logger.LogDebug("Got video {Title} by {Author}", video.Title, video.Author.ChannelTitle);
        var thumbnailUrl = video.Thumbnails.GetWithHighestResolution();
        var thumbnailData = await _httpClient.GetByteArrayAsync(thumbnailUrl.Url);
        var thumbnailMimeType = MimeGuesser.GuessMimeType(thumbnailData);
        var audioItem = new AudioItem(this, Parent, uri, videoId, video.Title, video.Author.ChannelTitle,
            video.Duration ?? TimeSpan.Zero, thumbnailData, thumbnailMimeType);
        return audioItem;
    }

    public ValueTask<VideoItem?> GetVideoItemAsync(string uri)
    {
        throw new NotImplementedException();
    }

    public ValueTask<SubtitleItem?> GetSubtitleItemAsync(string uri)
    {
        throw new NotImplementedException();
    }

    public ValueTask<AudioItem[]?> GetAudioItemPlaylistAsync(string uri)
    {
        throw new NotImplementedException();
    }

    public ValueTask<VideoItem[]?> GetVideoItemPlaylistAsync(string uri)
    {
        throw new NotImplementedException();
    }

    public ValueTask<SubtitleItem[]?> GetSubtitleItemPlaylistAsync(string uri)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> InitializeAsync(params string[] options)
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

    public async ValueTask<bool> CachePlayItemAsync(MediaItem item)
    {
        IStreamInfo streamInfo;
        var manifest = await _youtubeClient.Videos.Streams.GetManifestAsync(item.SourcePath);
        this._logger.LogDebug("Got manifest for {PathTitle}", item.PathTitle);
        switch (item)
        {
            case AudioItem:
                streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                this._logger.LogDebug("Got audio stream for {PathTitle}, bitrate: {Bitrate}", item.PathTitle,
                    streamInfo.Bitrate);
                break;
            case VideoItem:
                streamInfo = manifest.GetMuxedStreams().GetWithHighestVideoQuality();
                this._logger.LogDebug("Got video stream for {PathTitle}, size: {Size}", item.PathTitle,
                    streamInfo.Size);
                break;
            default:
                return false;
        }
        
        using var ms = new MemoryStream();
        _logger.LogInformation("Caching {PathTitle} to MemoryStream", item.PathTitle);
        await _youtubeClient.Videos.Streams.CopyToAsync(streamInfo, ms, new Progress<double>((progress) =>
        {
            this._logger.LogDebug("Progress for {PathTitle}: {Progress}", item.PathTitle, progress);
        }));
        this._logger.LogInformation("{PathTitle} has been cached to MemoryStream", item.PathTitle);
        var mimeType = MimeGuesser.GuessMimeType(ms.GetBuffer()); 
        item.MimeType = mimeType;
        var cacheNameExtension = item switch
        {
            AudioItem => "mcia",
            VideoItem => "mciv",
            _ => throw new ArgumentOutOfRangeException(nameof(item))
        };
        var cacheName = $"{item.OwnerId}_{item.PathTitle}.{cacheNameExtension}";
        this._logger.LogDebug("Saving {PathTitle} to {CacheName}", item.PathTitle, cacheName);
        return await _cacheStrategy.CacheAsync(item, ms.GetBuffer(), cacheName);
    }

    public ValueTask<bool> RemoveFromCacheAsync(MediaItem item)
    {
        return this._cacheStrategy.RemoveAsync(item);
    }

    public ValueTask<string?> GetCachedMediaItemPathAsync(MediaItem item)
    {
        return this._cacheStrategy.GetCachedPathAsync(item);
    }

    public ValueTask<Stream?> GetCachedMediaItemStreamAsync(MediaItem item)
    {
        return this._cacheStrategy.GetCachedStreamAsync(item);
    }
}