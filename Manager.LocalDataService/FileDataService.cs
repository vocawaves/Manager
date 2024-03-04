using HeyRed.Mime;
using Manager.Shared.Cache;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.Data;
using Manager.Shared.Events.General;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.Data;
using MetadataReader.FFMPEG;
using Microsoft.Extensions.Logging;

namespace Manager.LocalDataService;

public class FileDataService : IFileSystemSource
{
    public event AsyncEventHandler<InitSuccessEventArgs>? InitSuccess;
    public event AsyncEventHandler<InitFailedEventArgs>? InitFailed;
    public event AsyncEventHandler<CacheFailedEventArgs>? CacheFailed;

    public string MountName { get; }
    public bool Initialized { get; } = true;
    public string Name { get; }
    public ulong Parent { get; }

    public ICacheStrategy CacheStrategy => this._cacheStrategy;

    private readonly ILogger<FileDataService> _logger;

    private readonly BasicCacheStrategy _cacheStrategy = new();

    public FileDataService(ILoggerFactory loggerFactory, string mountName, string name, ulong parent)
    {
        this._logger = loggerFactory.CreateLogger<FileDataService>();
        this.MountName = mountName;
        this.Name = name;
        this.Parent = parent;
    }

    public ValueTask<bool> InitializeAsync(params string[] options)
    {
        return ValueTask.FromResult(true);
    }

    public async ValueTask<PlaybackItem?> GetPlayItemFromUriAsync(string uri)
    {
        if (!File.Exists(uri))
        {
            this._logger.LogError("File {path} does not exist", uri);
            return default;
        }

        var mimeData = MimeGuesser.GuessFileType(uri);
        this._logger.LogDebug("Guessed mime data for {uri}, got extension {ext} and mime type {mime}", uri,
            mimeData.Extension, mimeData.MimeType);
        var mimeType = mimeData.MimeType;
        var extension = mimeData.Extension == "bin" ? Path.GetExtension(uri).Replace(".", "") : mimeData.Extension;
        this._logger.LogDebug("Got extension {ext} for {uri}", extension, uri);
        var isAudio = mimeType.StartsWith("audio/");
        var isVideo = mimeType.StartsWith("video/");

        this._logger.LogDebug("Getting PlayItem from {uri}", uri);
        var metaData = FfmpegReader.ReadMetaDataTags(uri);
        this._logger.LogDebug("Got metadata for {uri}, got {count} tags", uri, metaData.Count);
        var title = metaData.GetValueOrDefault("title", Path.GetFileName(uri));
        this._logger.LogDebug("Got title {title} for {uri}", title, uri);
        var artist = metaData.GetValueOrDefault("artist", "Unknown");
        this._logger.LogDebug("Got artist {artist} for {uri}", artist, uri);
        var duration = FfmpegReader.GetDuration(uri);
        this._logger.LogDebug("Got duration {duration} for {uri}", duration, uri);
        var item = new PlaybackItem(this, uri, extension, mimeType, title, artist, duration, this.Parent);
        foreach (var (key, value) in metaData)
            item.Metadata[key] = value;

        if (isAudio)
        {
            var thumbnail = await FfmpegReader.TryReadCoverArt(uri);
            this._logger.LogDebug("Got cover art for {uri}, got {thumbnail}", uri, thumbnail != null);
            if (thumbnail == null)
                return item;
            var thumbMimeData = MimeGuesser.GuessFileType(thumbnail);
            this._logger.LogDebug("Guessed cover art mime data for {uri}, got extension {ext} and mime type {mime}",
                uri, thumbMimeData.Extension, thumbMimeData.MimeType);
            item.SetThumbnail(thumbnail, thumbMimeData.Extension, thumbMimeData.MimeType);
        }
        else if (isVideo)
        {
            var thumbnail = FfmpegReader.TryGetVideoThumbnail(uri, out var thumbData);
            this._logger.LogDebug("Got video thumbnail for {uri}, got {thumbnail}", uri, thumbnail);
            if (!thumbnail)
                return item;
            var thumbMimeData = MimeGuesser.GuessFileType(thumbData);
            this._logger.LogDebug(
                "Guessed video thumbnail mime data for {uri}, got extension {ext} and mime type {mime}", uri,
                thumbMimeData.Extension, thumbMimeData.MimeType);
            item.SetThumbnail(thumbData, thumbMimeData.Extension, thumbMimeData.MimeType);
        }

        return item;
    }

    public async ValueTask<PlaybackItem?> GetPlayItemAsync(FileItem item)
    {
        return await this.GetPlayItemFromUriAsync(item.Path);
    }

    public ValueTask<DirectoryItem[]> GetDirectoriesAsync(string? path = null)
    {
        path ??= this.MountName;
        if (!Directory.Exists(path))
            return ValueTask.FromResult(Array.Empty<DirectoryItem>());

        var dirs = Directory.GetDirectories(path);
        var items = new DirectoryItem[dirs.Length];
        for (var i = 0; i < dirs.Length; i++)
        {
            var dir = dirs[i];
            items[i] = new DirectoryItem(Path.GetDirectoryName(dir) ?? "<Invalid Dir Data>", dir,
                path == this.MountName);
        }

        return ValueTask.FromResult(items);
    }

    public ValueTask<FileItem[]> GetFilesAsync(DirectoryItem? item = null, params string[] extensions)
    {
        var path = item?.FullPath ?? this.MountName;
        if (!Directory.Exists(path))
            return ValueTask.FromResult(Array.Empty<FileItem>());

        var files = Directory.GetFiles(path);
        var items = new FileItem[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            var file = files[i];
            items[i] = new FileItem(Path.GetFileName(file), file);
        }

        return ValueTask.FromResult(items.ToArray());
    }

    public ValueTask<PlaybackItem[]> GetPlaylistFromFileAsync(FileItem item)
    {
        throw new NotImplementedException();
    }

    public async ValueTask<bool> CachePlayItemAsync(PlaybackItem item)
    {
        var cacheName = $"{item.OwnerId}_{item.Title}_{item.Extension}";
        if (item.IsCached)
            return true;
        
        await this.CacheStrategy.CacheAsync(item, item.OwnerPath, cacheName);
        item.IsCached = true;
        return true;
    }

    public ValueTask<bool> RemoveFromCacheAsync(PlaybackItem item)
    {
        if (!item.IsCached)
            return ValueTask.FromResult(false);
        
        return this.CacheStrategy.RemoveAsync(item);
    }
}