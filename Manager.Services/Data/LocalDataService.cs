using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using HeyRed.Mime;
using Manager.Services.Utilities;
using Manager.Shared;
using Manager.Shared.Entities;
using Manager.Shared.Interfaces;
using Sdcb.FFmpeg.Raw;

namespace Manager.Services.Data;

public class LocalDataService : ManagerComponent, IDataService
{
    private readonly ConcurrentDictionary<string, PlayItem> _cache = new();

    public LocalDataService(string name, ulong parent)
        : base(name, parent)
    {
        this.Initialized = true;
    }

    public ValueTask<string[]> GetDirectoriesAsync(string? path = null)
    {
        if (path is not null && !Directory.Exists(path))
        {
            this.SendError(this, nameof(this.GetDirectoriesAsync),
                "Directory does not exist", path);
            return ValueTask.FromResult(Array.Empty<string>());
        }

        try
        {
            return ValueTask.FromResult(path is null
                ? DriveInfo.GetDrives().Select(drive => drive.Name).ToArray()
                : Directory.GetDirectories(path));
        }
        catch (Exception e)
        {
            this.SendError(this, nameof(this.GetDirectoriesAsync),
                e,
                path ?? "null");
            return ValueTask.FromResult(Array.Empty<string>());
        }
    }

    public ValueTask<string[]> GetFilesAsync(string? path = null, params string[] extensions)
    {
        try
        {
            path ??= DriveInfo.GetDrives()[0].Name;
            var extAsPattern = extensions.Length != 0 ? $"*.{string.Join("*.", extensions)}" : "*.*";
            var files = Directory.GetFiles(path, extAsPattern, SearchOption.TopDirectoryOnly);
            return ValueTask.FromResult(files);
        }
        catch (Exception e)
        {
            this.SendError(this, nameof(this.GetFilesAsync),
                e,
                path ?? "null",
                string.Join(", ", extensions));
            return ValueTask.FromResult(Array.Empty<string>());
        }
    }

    public async ValueTask<PlayItem?> GetPlayItemAsync(string path)
    {
        if (!File.Exists(path))
        {
            this.SendError(this, nameof(this.GetPlayItemAsync),
                "File does not exist", path);
            return null;
        }

        if (this._cache.TryGetValue(path, out var value))
            return null;

        //read first 10MB of file to determine file type
        await using var tempBytes = File.OpenRead(path);
        var tempBuffer = new byte[10 * 1024 * 1024];
        _ = await tempBytes.ReadAsync(tempBuffer, 0, tempBuffer.Length);
        var fileType = MimeGuesser.GuessFileType(tempBuffer);
        var item = new PlayItem
        {
            AssociatedDataService = this,
            OwnerPath = path,
            OwnerId = this.Parent,
            Extension = fileType.Extension,
            MimeType = fileType.MimeType,
        };

        item = GetMetaData(item);
        
        if (item.Thumbnail is null)
            item = await TryFindCoverImage(item);

        if (string.IsNullOrWhiteSpace(item.Title))
            item.Title = Path.GetFileName(item.OwnerPath);

        this._cache.AddOrUpdate(item.OwnerPath, item, (_, _) => item);
        return item;
    }

    public unsafe PlayItem GetMetaData(PlayItem item)
    {
        //prepare ffmpeg
        AVFormatContext* formatContext = null;
        var result = ffmpeg.avformat_open_input(&formatContext, item.OwnerPath, null, null);
        if (result < 0)
        {
            this.SendError(this, nameof(this.GetMetaData),
                "Failed to open file",
                item.OwnerPath);
        }

        //get stream info
        result = ffmpeg.avformat_find_stream_info(formatContext, null);
        if (result < 0)
        {
            this.SendError(this, nameof(this.GetMetaData),
                "Failed to find stream info",
                item.OwnerPath);
        }

        if (TryReadTags(formatContext, out var tags))
        {
            if (tags.TryGetValue("title", out var title))
                item.Title = title;
            if (tags.TryGetValue("artist", out var artist) || tags.TryGetValue("album_artist", out artist))
                item.Artist = artist;
        }
        
        if (TryReadDuration(formatContext, out var duration))
            item.Duration = duration;

        var isVideo = item.MimeType.StartsWith("video");
        if (isVideo)
        {
            //free memory
            ffmpeg.avformat_close_input(&formatContext);
            ffmpeg.avformat_free_context(formatContext);
            return item;
        }
        
        if (TryGetCoverArt(formatContext, out var coverArtBytes))
        {
            item.Thumbnail = coverArtBytes;
            var typeInfo = MimeGuesser.GuessFileType(coverArtBytes);
            item.ThumbnailExtension = typeInfo.Extension;
            item.ThumbnailMimeType = typeInfo.MimeType;
        }
        else
        {
            //try to find cover art in the directory
            var dir = Path.GetDirectoryName(item.OwnerPath);
            if (dir is not null)
            {
                var files = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(file => (file.EndsWith(".jpg")
                                    || file.EndsWith(".jpeg")
                                    || file.EndsWith(".png")
                                    || file.EndsWith(".bmp")
                                    || file.EndsWith(".gif"))
                                   && (file.ToLower().Contains("cover")
                                       || file.ToLower().Contains("thumb")));
                var imageFile = files.MaxBy(file => new FileInfo(file).Length);
                if (imageFile is not null)
                {
                    var imageBytes = File.ReadAllBytes(imageFile);
                    var typeInfo = MimeGuesser.GuessFileType(imageBytes);
                    item.Thumbnail = imageBytes;
                    item.ThumbnailExtension = typeInfo.Extension;
                    item.ThumbnailMimeType = typeInfo.MimeType;
                }
            }
        }
        
        //free memory
        ffmpeg.avformat_close_input(&formatContext);
        ffmpeg.avformat_free_context(formatContext);
        
        return item;
    }

    private unsafe bool TryReadTags(AVFormatContext* formatContext, out Dictionary<string, string> tags)
    {
        tags = new Dictionary<string, string>();

        AVDictionaryEntry* tag = null;
        while ((tag = ffmpeg.av_dict_get(formatContext->metadata, "", tag, 0)) != null)
        {
            var key = Marshal.PtrToStringUTF8((IntPtr)tag->key);
            if (key is null)
                continue;
            var value = Marshal.PtrToStringUTF8((IntPtr)tag->value);
            if (value is null)
                continue;
            //normalize key
            key = key.ToLower().Replace(" ", "");
            tags.Add(key, value);
        }

        //free memory(?)
        //var tagPtr = formatContext->metadata;
        //ffmpeg.av_dict_free(&tagPtr);
        return true;
    }

    private unsafe bool TryReadDuration(AVFormatContext* formatContext, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;
        var streamIndex = -1;
        //find first stream with duration > 0
        for (var i = 0; i < formatContext->nb_streams; i++)
        {
            if (formatContext->streams[i]->duration <= 0)
                continue;
            streamIndex = i;
            break;
        }

        if (streamIndex == -1)
            return false;
        
        var foundStream = formatContext->streams[streamIndex];
        var timeBase = foundStream->time_base;
        var timeBaseDouble = ffmpeg.av_q2d(timeBase);
        var durationTicks = foundStream->duration * timeBaseDouble;
        duration = TimeSpan.FromSeconds(durationTicks);
        return true;
    }

    private unsafe bool TryGetCoverArt(AVFormatContext* formatContext, out byte[] coverArtBytes)
    {
        coverArtBytes = Array.Empty<byte>();
        
        // Find the first audio stream
        var audioStreamIndex = -1;
        for (var i = 0; i < formatContext->nb_streams; i++)
        {
            if (formatContext->streams[i]->codecpar->codec_type != AVMediaType.Audio)
                continue;
            audioStreamIndex = i;
            break;
        }

        if (audioStreamIndex == -1)
            return false;

        //find the cover art stream of the m4a file
        //steam 0:0 is the audio stream
        //stream 0:1 is the cover art stream, which we want to extract
        var coverArtStreamIndex = -1;
        for (var i = 0; i < formatContext->nb_streams; i++)
        {
            if (formatContext->streams[i]->codecpar->codec_type != AVMediaType.Video)
                continue;
            coverArtStreamIndex = i;
            break;
        }

        if (coverArtStreamIndex == -1)
            return false;

        //get cover art data
        var coverArtPacket = ffmpeg.av_packet_alloc();
        var result = ffmpeg.av_read_frame(formatContext, coverArtPacket);
        if (result < 0)
            return false;

        coverArtBytes = new byte[coverArtPacket->size];
        Marshal.Copy((IntPtr)coverArtPacket->data, coverArtBytes, 0, coverArtBytes.Length);

        //free memory
        ffmpeg.av_packet_unref(coverArtPacket);
        ffmpeg.av_packet_free(&coverArtPacket);

        return true;
    }

    private async Task<PlayItem> TryFindCoverImage(PlayItem item)
    {
        try
        {
            //check if there is a separate thumbnail or cover image file "*.jpg *.jpeg *.png *.bmp *.gif"
            var dir = Path.GetDirectoryName(item.OwnerPath);
            if (dir is not null)
            {
                var files = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(file => (file.EndsWith(".jpg")
                                    || file.EndsWith(".jpeg")
                                    || file.EndsWith(".png")
                                    || file.EndsWith(".bmp")
                                    || file.EndsWith(".gif"))
                                   && (file.ToLower().Contains("cover")
                                       || file.ToLower().Contains("thumb")));
                var imageFile = files.MaxBy(file => new FileInfo(file).Length);
                if (imageFile is not null)
                {
                    var imageBytes = await File.ReadAllBytesAsync(imageFile);
                    var typeInfo = MimeGuesser.GuessFileType(imageBytes);
                    item.Thumbnail = await File.ReadAllBytesAsync(imageFile);
                    item.ThumbnailExtension = typeInfo.Extension;
                    item.ThumbnailMimeType = typeInfo.MimeType;
                }
            }
        }
        catch
        {
            /* ignored */
        }

        return item;
    }

    public async ValueTask<PlayItem?> CachePlayItemAsync(PlayItem item)
    {
        if (item.Cached)
            return item;

        if (item.OwnerId != this.Parent)
        {
            this.SendError(this, nameof(this.CachePlayItemAsync),
                "PlayItem does not belong to this client",
                item.OwnerId, this.Parent, item.OwnerPath);
            return default;
        }

        if (!File.Exists(item.OwnerPath))
        {
            this.SendError(this, nameof(this.CachePlayItemAsync),
                "File does not exist",
                item.OwnerPath);
            return default;
        }

        if (_cache.TryGetValue(item.OwnerPath, out var value) && value.Cached)
            return value;

        try
        {
            var fileData = await File.ReadAllBytesAsync(item.OwnerPath);
            item.Data = fileData;
            item.Cached = true;
            this._cache.AddOrUpdate(item.OwnerPath, item, (_, _) => item);
            return item;
        }
        catch (Exception e)
        {
            this.SendError(this, nameof(this.CachePlayItemAsync),
                e,
                item.OwnerPath);
            return null;
        }
    }

    public ValueTask<bool> RemovePlayItemFromCacheAsync(string path)
    {
        if (!_cache.ContainsKey(path))
        {
            this.SendError(this, nameof(this.RemovePlayItemFromCacheAsync),
                "PlayItem does not exist in cache",
                path);
            //Still return true because the item is not in the cache
            return ValueTask.FromResult(true);
        }

        var result = _cache.TryRemove(path, out _);
        if (!result)
        {
            this.SendError(this, nameof(this.RemovePlayItemFromCacheAsync),
                "Failed to remove PlayItem from cache",
                path);
        }

        return ValueTask.FromResult(result);
    }

    public ValueTask<bool> RemovePlayItemFromCacheAsync(PlayItem item)
        => RemovePlayItemFromCacheAsync(item.OwnerPath);
}