using HeyRed.Mime;
using Manager.Shared;
using Manager.Shared.Cache;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces.Data;
using Manager.Shared.Interfaces.General;
using Microsoft.Extensions.Logging;

namespace Manager.DataBackends.Local;

public class LocalDataService : IManagerComponent<LocalDataServiceConfiguration>, IFileSystemSource, IAudioDataSource, IVideoDataSource, ISubtitleDataSource,
    IImageDataSource, IMiscDataSource
{

    #region IManagerComponent
    public ComponentManager ComponentManager { get; }
    public string Name { get; }
    public ulong Parent { get; }
    
    #endregion

    public LocalDataServiceConfiguration Configuration { get; }
    
    private readonly ILogger<LocalDataService>? _logger;
    private readonly ICacheStrategy _cacheStrategy;

    public LocalDataService(ComponentManager componentManager, string name, ulong parent)
    {
        _logger = componentManager.CreateLogger<LocalDataService>();
        ComponentManager = componentManager;
        Name = name;
        Parent = parent;
        var strategyConfig = new FolderCacheStrategyConfiguration
        {
            CacheFolder = Path.Combine(Directory.GetCurrentDirectory(), "ManagerCache")
        };
        var strategy = componentManager.CreateComponent<FolderCacheStrategy, FolderCacheStrategyConfiguration>("LocalCacheStrategy", parent, strategyConfig);
        if (strategy is null)
        {
            _logger?.LogError("Failed to create cache strategy");
            throw new InvalidOperationException("Failed to create cache strategy");
        }
        Configuration = new LocalDataServiceConfiguration
        {
            CacheStrategy = strategy
        };
        _cacheStrategy = strategy;
        MimeTypesMap.AddOrUpdate("audio/mpeg", ".mp3");
        MimeTypesMap.AddOrUpdate("audio/ogg", ".ogg");
        MimeTypesMap.AddOrUpdate("audio/opus", ".opus");
        MimeTypesMap.AddOrUpdate("audio/mpeg", ".m4a");
    }
    
    public LocalDataService(ComponentManager componentManager, string name, ulong parent, LocalDataServiceConfiguration configuration)
    {
        _logger = componentManager.CreateLogger<LocalDataService>();
        ComponentManager = componentManager;
        Name = name;
        Parent = parent;
        Configuration = configuration;
        _cacheStrategy = configuration.CacheStrategy;
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

    public async ValueTask<MediaItem?> GetMediaItemAsync(string uri, ItemType type = ItemType.Guess)
    {
        if (!File.Exists(uri))
        {
            _logger?.LogError("File not found: {0}", uri);
            return null;
        }

        if (type == ItemType.Guess)
        {
            var typeGuess = await GetMimeType(uri);
            var byExtension = MimeTypesMap.GetMimeType(Path.GetExtension(uri));
            if (typeGuess.StartsWith("video/") || byExtension.StartsWith("video/"))
                type = ItemType.Video;
            else if (typeGuess.StartsWith("audio/") || byExtension.StartsWith("audio/"))
                type = ItemType.Audio;
            else if (typeGuess.StartsWith("image/") || byExtension.StartsWith("image/"))
                type = ItemType.Image;
            else if (typeGuess.StartsWith("text/") || byExtension.StartsWith("text/"))
                type = ItemType.Subtitle;
            else
                type = ItemType.Misc;
        }
        
        var pathTitle = Path.GetFileName(uri);
        var item = new MediaItem(this, type, Parent, uri, pathTitle, this.ComponentManager.CreateLogger<MediaItem>());
        return item;
    }

    public ValueTask<MediaItem[]?> GetMediaItemsAsync(string uri, ItemType type = ItemType.Guess)
    {
        throw new NotImplementedException();
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

    public async ValueTask<bool> CacheMediaItemAsync(MediaItem item)
    {
        if (item.ItemType == ItemType.Guess)
        {
            _logger?.LogError("Cannot cache media item with ItemType.Guess");
            return false;
        }

        if (item.CacheState is CacheState.Cached or CacheState.DiskCaching or CacheState.Downloading)
        {
            _logger?.LogInformation("Media item already cached: {0}", item.SourcePath);
            return true;
        }

        var cacheExtension = item.ItemType switch
        {
            ItemType.Video => ".mcv", //If its a Video and Audio and/or Subtitle, use the video extension (.mcv) as the cache extension
            ItemType.Audio => ".mca", //If its only an Audio file, use the audio extension (.mca) as the cache extension
            ItemType.Subtitle => ".mcs", //If its only a Subtitle file, use the subtitle extension (.mcs) as the cache extension
            ItemType.Image => ".mci",
            ItemType.Misc => ".mcm",
            _ => throw new InvalidOperationException($"Invalid ItemType: {item.ItemType}, source path: {item.SourcePath}")
        };
        _logger?.LogInformation("Determined cache extension: {0}", cacheExtension);
        var cacheName = $"{item.PathTitle}{cacheExtension}";
        _logger?.LogInformation("Using cache name: {0}", cacheName);
        await _cacheStrategy.CacheAsync(item, item.SourcePath, cacheName);
        return true;
    }

    public ValueTask<bool> RemoveMediaItemFromCacheAsync(MediaItem item)
    {
        return _cacheStrategy.RemoveAsync(item);
    }

    public ValueTask<string?> GetCachedMediaItemPathAsync(MediaItem item)
    {
        return _cacheStrategy.GetCachedPathAsync(item);
    }

    public ValueTask<Stream?> GetCachedMediaItemStreamAsync(MediaItem item)
    {
        return _cacheStrategy.GetCachedStreamAsync(item);
    }
}