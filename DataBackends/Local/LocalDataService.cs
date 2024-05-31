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

namespace Manager.DataBackends.Local;

public class LocalDataService : IManagerComponent<LocalDataServiceConfiguration>, IFileSystemSource, IAudioDataSource, IVideoDataSource, ISubtitleDataSource,
    IImageDataSource, IMiscDataSource
{
    private readonly ILogger<LocalDataService>? _logger;
    private readonly ICacheStrategy _cacheStrategy;

    #region IManagerComponent
    
    public event AsyncEventHandler? InitSuccess;
    public event AsyncEventHandler<InitFailedEventArgs>? InitFailed;
    
    public bool Initialized { get; } = true;
    public ComponentManager ComponentManager { get; }
    public string Name { get; }
    public ulong Parent { get; }
    
    #endregion

    private LocalDataService(ComponentManager componentManager, string name, ulong parent, IComponentConfiguration? configuration = null)
    {
        ComponentManager = componentManager;
        Name = name;
        Parent = parent;
        if (configuration is not LocalDataServiceConfiguration localConfig || localConfig.CacheStrategy is null)
        {
            var strategy = FolderCacheStrategy.Create(componentManager?.CreateLogger<FolderCacheStrategy>());
            _cacheStrategy = strategy ?? throw new InvalidOperationException("Failed to create cache strategy");
        }
        else
        {
            _cacheStrategy = localConfig.CacheStrategy;
        }
        _logger = this.ComponentManager.CreateLogger<LocalDataService>();
    }

    public static IManagerComponent? Create(ComponentManager componentManager, string name, ulong parent)
    {
        return new LocalDataService(componentManager, name, parent);
    }
    
    public static IManagerComponent<LocalDataServiceConfiguration>? CreateWithConfiguration(ComponentManager componentManager, string name, ulong parent, LocalDataServiceConfiguration configuration)
    {
        return new LocalDataService(componentManager, name, parent, configuration);
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
            if (typeGuess.StartsWith("video/"))
                type = ItemType.Video;
            else if (typeGuess.StartsWith("audio/"))
                type = ItemType.Audio;
            else if (typeGuess.StartsWith("image/"))
                type = ItemType.Image;
            else if (typeGuess.StartsWith("text/"))
                type = ItemType.Subtitle;
            else
                type = ItemType.Misc;
        }
        
        var pathTitle = Path.GetFileNameWithoutExtension(uri);
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

    public LocalDataServiceConfiguration? Configuration { get; }
}