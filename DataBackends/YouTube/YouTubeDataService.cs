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

public class YouTubeDataService : IStreamingServiceSource, IAudioDataSource, IVideoDataSource
{
    public event AsyncEventHandler<InitSuccessEventArgs>? InitSuccess;
    public event AsyncEventHandler<InitFailedEventArgs>? InitFailed;

    public bool Initialized { get; } = true;
    public string Name { get; }
    public ulong Parent { get; }

    private readonly ILogger<YouTubeDataService>? _logger;
    private readonly ICacheStrategy _cacheStrategy;
    private readonly YoutubeClient _youtubeClient;
    private readonly HttpClient _httpClient;
    private readonly Instancer _instancer;

    private YouTubeDataService(Instancer instancer, string name, ulong parent)
    {
        _instancer = instancer;
        _cacheStrategy = new BasicCacheStrategy(instancer.CreateLogger<BasicCacheStrategy>());
        _logger = instancer.CreateLogger<YouTubeDataService>();
        Name = name;
        Parent = parent;
        _youtubeClient = new YoutubeClient();
        _httpClient = new HttpClient();
    }

    public static IManagerComponent Create(Instancer instancer, string name, ulong parent)
    {
        return new YouTubeDataService(instancer, name, parent);
    }


    public async ValueTask<AudioItem?> GetAudioItemAsync(string uri)
    {
        var videoId = VideoId.TryParse(uri);
        if (videoId is null)
        {
            _logger?.LogError("Failed to parse video id from uri {Uri}", uri);
            return null;
        }

        var video = await _youtubeClient.Videos.GetAsync(videoId.Value);
        this._logger?.LogDebug("Got video {Title} by {Author}", video.Title, video.Author.ChannelTitle);
        var thumbnailUrl = video.Thumbnails.GetWithHighestResolution();
        var thumbnailData = await _httpClient.GetByteArrayAsync(thumbnailUrl.Url);
        var thumbnailMimeType = MimeGuesser.GuessMimeType(thumbnailData);
        var audioItem = new AudioItem(this, Parent, uri, videoId, video.Title, video.Author.ChannelTitle,
            video.Duration ?? TimeSpan.Zero, thumbnailData, thumbnailMimeType,
            this._instancer.CreateLogger<AudioItem>());
        return audioItem;
    }

    public async ValueTask<VideoItem?> GetVideoItemAsync(string uri)
    {
        var videoId = VideoId.TryParse(uri);
        if (videoId is null)
        {
            _logger?.LogError("Failed to parse video id from uri {Uri}", uri);
            return null;
        }

        var video = await _youtubeClient.Videos.GetAsync(videoId.Value);
        this._logger?.LogDebug("Got video {Title} by {Author}", video.Title, video.Author.ChannelTitle);
        var thumbnailUrl = video.Thumbnails.GetWithHighestResolution();
        var thumbnailData = await _httpClient.GetByteArrayAsync(thumbnailUrl.Url);
        var thumbnailMimeType = MimeGuesser.GuessMimeType(thumbnailData);
        var videoItem = new VideoItem(this, Parent, uri, videoId, video.Duration ?? TimeSpan.Zero, thumbnailData,
            thumbnailMimeType, this._instancer.CreateLogger<VideoItem>());
        return videoItem;
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
        return ValueTask.FromResult(true);
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
        item.SetCacheState(CacheState.Caching);


        var cacheNameExtension = item switch
        {
            AudioItem => "mcia",
            VideoItem => "mciv",
            _ => throw new ArgumentOutOfRangeException(nameof(item))
        };
        var cacheName = $"{item.OwnerId}_{item.PathTitle}.{cacheNameExtension}";
        var alreadyCached = await _cacheStrategy.CheckForOldCacheAsync(item, cacheName);
        if (alreadyCached)
        {
            this._logger?.LogDebug("{PathTitle} is already cached", item.PathTitle);
            return true;
        }

        IStreamInfo streamInfo;
        var manifest = await _youtubeClient.Videos.Streams.GetManifestAsync(item.SourcePath);
        this._logger?.LogDebug("Got manifest for {PathTitle}", item.PathTitle);
        switch (item)
        {
            case AudioItem:
                streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                this._logger?.LogDebug("Got audio stream for {PathTitle}, bitrate: {Bitrate}", item.PathTitle,
                    streamInfo.Bitrate);
                break;
            case VideoItem:
                streamInfo = manifest.GetVideoStreams().GetWithHighestVideoQuality();
                this._logger?.LogDebug("Got video stream for {PathTitle}, size: {Size}", item.PathTitle,
                    streamInfo.Size);
                break;
            default:
                this._logger?.LogError("Unknown item type {ItemType}", item.GetType().Name);
                item.SetCacheState(CacheState.Failed);
                return false;
        }

        using var ms = new MemoryStream();
        _logger?.LogInformation("Caching {PathTitle} to MemoryStream", item.PathTitle);
        await _youtubeClient.Videos.Streams.CopyToAsync(streamInfo, ms,
            new Progress<double>((progress) => { item.SetCacheProgress(progress * 100); }));
        this._logger?.LogInformation("{PathTitle} has been cached to MemoryStream", item.PathTitle);
        var mimeType = MimeGuesser.GuessMimeType(ms.GetBuffer());
        item.MimeType = mimeType;
        this._logger?.LogDebug("Saving {PathTitle} to {CacheName}", item.PathTitle, cacheName);
        return await _cacheStrategy.CacheAsync(item, ms, cacheName);
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