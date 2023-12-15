using ATL;
using HeyRed.Mime;
using Manager.Shared;
using Manager.Shared.Entities;
using Manager.Shared.Interfaces;

namespace Manager.Services.Data;

public class LocalDataService : ManagerComponent, IDataService
{
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

    public ValueTask<PlayItem?> GetPlayItemAsync(string path)
    {
        if (!File.Exists(path))
        {
            this.SendError(this, nameof(this.GetPlayItemAsync),
                "File does not exist", path);
            return ValueTask.FromResult<PlayItem?>(null);
        }

        var playItem = new PlayItem
        {
            OwnerPath = path,
            OwnerId = this.Parent,
            Format = MimeGuesser.GuessExtension(path),
            Title = Path.GetFileName(path),
        };
        return ValueTask.FromResult((PlayItem?)playItem);
    }

    public async ValueTask<bool> CachePlayItemAsync(PlayItem item)
    {
        if (item.Cached)
            return true;

        if (item.OwnerId != this.Parent)
        {
            this.SendError(this, nameof(this.CachePlayItemAsync),
                "PlayItem does not belong to this client",
                item.OwnerId, this.Parent, item.OwnerPath);
            return false;
        }

        if (!File.Exists(item.OwnerPath))
        {
            this.SendError(this, nameof(this.CachePlayItemAsync),
                "File does not exist",
                item.OwnerPath);
            return false;
        }

        try
        {
            var fileData = await File.ReadAllBytesAsync(item.OwnerPath);
            item.Data = fileData;
            item.Cached = true;

            //Try to read tags
            try
            {
                var track = new Track(item.OwnerPath);
                item.Title = track.Title;
                item.Artist = track.Artist;
                item.Duration = TimeSpan.FromMilliseconds(track.DurationMs);

                if (track.EmbeddedPictures.Count != 0)
                {
                    var picture = track.EmbeddedPictures[0];
                    item.Thumbnail = picture.PictureData;
                    item.ThumbnailFormat = MimeGuesser.GuessExtension(picture.PictureData);
                }
            }
            catch
            {
                /* ignored */
            }

            return true;
        }
        catch (Exception e)
        {
            this.SendError(this, nameof(this.CachePlayItemAsync),
                e,
                item.OwnerPath);
            return false;
        }
    }
}