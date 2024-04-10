using HeyRed.Mime;
using Manager.Shared.Cache;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.General;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.Data;
using MetadataReader.FFMPEG;
using Microsoft.Extensions.Logging;

namespace Manager.LocalDataService;

public class LocalDataService : IFileSystemSource, IAudioDataSource, IVideoDataSource, ISubtitleDataSource,
    IImageDataSource, IMiscDataSource
{
    public event AsyncEventHandler<InitSuccessEventArgs>? InitSuccess;
    public event AsyncEventHandler<InitFailedEventArgs>? InitFailed;
    
    public bool Initialized { get; } = true;
    public string Name { get; }
    public ulong Parent { get; }


    private readonly ILogger<LocalDataService>? _logger;
    private readonly ICacheStrategy _cacheStrategy;
    private readonly ILoggerFactory? _loggerFactory;

    public LocalDataService(string name, ulong parent, ILoggerFactory? lf = null)
    {
        _loggerFactory = lf;
        _cacheStrategy = new BasicCacheStrategy(lf);
        _logger = lf?.CreateLogger<LocalDataService>();
        Name = name;
        Parent = parent;
    }

    public ValueTask<bool> InitializeAsync(params string[] options)
    {
        return ValueTask.FromResult(true);
    }
    
    public ValueTask<DirectoryItem[]> GetMountPointsAsync()
    {
        var drives = DriveInfo.GetDrives();
        var items = new DirectoryItem[drives.Length];
        for (var i = 0; i < drives.Length; i++)
        {
            var drive = drives[i];
            items[i] = new DirectoryItem($"({drive.Name}) {drive.VolumeLabel}", drive.Name);
        }
        return ValueTask.FromResult(items);
    }

    public ValueTask<FileItem[]> GetFilesAsync(string uri, params string[] extensions)
    {
        if (!Directory.Exists(uri))
        {
            _logger?.LogError("Directory not found: {0}", uri);
            return ValueTask.FromResult(Array.Empty<FileItem>());
        }

        var files = Directory.GetFiles(uri);
        var items = new FileItem[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            var file = files[i];
            items[i] = new FileItem(Path.GetFileName(file), file);
        }
        return ValueTask.FromResult(items);
    }

    public ValueTask<DirectoryItem[]> GetDirectoriesAsync(string uri)
    {
        if (!Directory.Exists(uri))
        {
            _logger?.LogError("Directory not found: {0}", uri);
            return ValueTask.FromResult(Array.Empty<DirectoryItem>());
        }

        var directories = Directory.GetDirectories(uri);
        var items = new DirectoryItem[directories.Length];
        for (var i = 0; i < directories.Length; i++)
        {
            var directory = directories[i];
            var dirInfo = new DirectoryInfo(directory);
            items[i] = new DirectoryItem(dirInfo.Name, directory);
        }
        return ValueTask.FromResult(items);
    }

    public async ValueTask<AudioItem?> GetAudioItemAsync(string uri)
    {
        if (!File.Exists(uri))
        {
            _logger?.LogError("File not found: {0}", uri);
            return null;
        }

        var mimeType = await GetMimeType(uri);
        if (!mimeType.StartsWith("audio/"))
        {
            this._logger?.LogWarning("File {0} is not an audio file, mime type: {1}", uri, mimeType);
        }

        var metadata = FfmpegReader.ReadMetaDataTags(uri);
        var duration = FfmpegReader.GetDuration(uri);
        var title = metadata.GetValueOrDefault("title", Path.GetFileNameWithoutExtension(uri));
        var artist = metadata.GetValueOrDefault("artist", "Unknown");
        var albumArt = await FfmpegReader.TryReadCoverArt(uri);
        if (albumArt is null)
        {
            _logger?.LogInformation("No album art found for {0}", uri);
            return new AudioItem(this, Parent, uri, Path.GetFileName(uri), title, artist, duration, this._loggerFactory)
            {
                MimeType = mimeType,
                Metadata = metadata
            };
        }
        
        var albumArtMimeType = MimeGuesser.GuessMimeType(albumArt);
        return new AudioItem(this, Parent, uri, Path.GetFileName(uri), title, artist, duration, albumArt, albumArtMimeType, this._loggerFactory)
        {
            MimeType = mimeType,
            Metadata = metadata
        };
    }

    public ValueTask<VideoItem?> GetVideoItemAsync(string uri)
    {
        throw new NotImplementedException();
    }

    public ValueTask<SubtitleItem?> GetSubtitleItemAsync(string uri)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ImageItem?> GetImageItemAsync(string uri)
    {
        throw new NotImplementedException();
    }

    public ValueTask<MediaItem?> GetMiscItemAsync(string uri)
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

    public ValueTask<ImageItem[]?> GetImageItemPlaylistAsync(string uri)
    {
        throw new NotImplementedException();
    }

    public ValueTask<MediaItem[]?> GetMiscItemPlaylistAsync(string uri)
    {
        throw new NotImplementedException();
    }

    public async ValueTask<bool> CachePlayItemAsync(MediaItem item)
    {
        if (item.CacheState == CacheState.Cached)
        {
            _logger?.LogInformation("Item {0} is already cached", item.SourcePath);
            return true;
        }
        
        if (item.CacheState == CacheState.Caching)
        {
            _logger?.LogInformation("Item {0} is already being cached", item.SourcePath);
            return true;
        }
        
        if (!File.Exists(item.SourcePath))
        {
            _logger?.LogError("File not found: {0}", item.SourcePath);
            return false;
        }
        
        string itemCacheExtension = item switch
        {
            AudioItem => "mcia",
            VideoItem => "mciv",
            SubtitleItem => "mcis",
            ImageItem => "mcii",
            _ => "mcig"
        };

        item.SetCacheState(CacheState.Caching);
        var cacheName = $"{item.OwnerId}_{item.PathTitle}.{itemCacheExtension}";
        return await this._cacheStrategy.CacheAsync(item, item.SourcePath, cacheName);
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

    private async ValueTask<string> GetMimeType(string uri)
    {
        var buffer = new byte[256];
        await using var fs = new FileStream(uri, FileMode.Open, FileAccess.Read);
        _ = await fs.ReadAsync(buffer, 0, 256);
        try
        {
            return MimeGuesser.GuessMimeType(buffer);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to guess mime type for {0}", uri);
            return "application/octet-stream";
        }
    }
}