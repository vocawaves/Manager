using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Manager.Shared;
using Manager.Shared.Extensions;
using Manager.Shared.Interfaces.Data;
using Manager.Shared.Interfaces.General;
using Microsoft.Extensions.Logging;
using SimplePlayer.Models;

namespace SimplePlayer.Utilities;

public class PlaylistIO : IManagerComponent
{
    private readonly ILogger<PlaylistIO>? _logger;
    public ComponentManager ComponentManager { get; }
    public string Name { get; }
    public ulong Parent { get; }

    public PlaylistIO(ComponentManager componentManager, string name, ulong parent)
    {
        _logger = componentManager.CreateLogger<PlaylistIO>();
        ComponentManager = componentManager;
        Name = name;
        Parent = parent;
    }
    
    /* Playlist Format:
     * Basically M3U, first line same as M3U so "EXTM3U" is required
     * Then "EXTPLNAME" for the playlist name
     * Then just the paths to the files 
     */
    
    public async ValueTask<PlaylistModel?> LoadPlaylist(string path)
    {
        if (!File.Exists(path))
        {
            _logger?.LogError("Playlist file does not exist: {Path}", path);
            return null;
        }
        
        var lines = await File.ReadAllLinesAsync(path);
        return await GetPlaylistFromTextLines(lines, Path.GetFileNameWithoutExtension(path));
    }

    public async ValueTask<List<PlaylistModel>?> LoadPlaylistCollection(string path)
    {
        //Just a zip file with multiple playlist files
        if (!File.Exists(path))
        {
            _logger?.LogError("Playlist collection file does not exist: {Path}", path);
            return null;
        }

        using var zip = ZipFile.Open(path, ZipArchiveMode.Read);
        var playlists = new List<PlaylistModel>();
        foreach (var entry in zip.Entries)
        {
            if (!entry.FullName.EndsWith(".m3u")) 
                continue;
            await using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var lines = await reader.ReadToEndAsync();
            var playlist = await GetPlaylistFromTextLines(lines.Split(Environment.NewLine), Path.GetFileNameWithoutExtension(entry.FullName));
            if (playlist != null)
                playlists.Add(playlist);
        }
        return playlists;
    }

    private async Task<PlaylistModel?> GetPlaylistFromTextLines(string[] lines, string defaultTitle = "Unnamed")
    {
        if (lines.Length < 2 || lines[0] != "#EXTM3U")
        {
            _logger?.LogError("Invalid playlist file, missing #EXTM3U");
            return null;
        }
        
        string name;
        if (lines[1].StartsWith("#EXTPLNAME:"))
        {
            name = lines[1].Substring(11);
            _logger?.LogInformation("Playlist name: {Name}", name);
        }
        else
        {
            name = defaultTitle;
            _logger?.LogWarning("Playlist name not found in file using default: {Name}", name);
        }
        var playlist = new PlaylistModel(name);
        var dataManager = ComponentManager.Components.OfType<IFileSystemSource>().FirstOrDefault();
        if (dataManager == null)
        {
            _logger?.LogError("No data manager found");
            return null;
        }
        for (var i = 2; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;
            if (lines[i].StartsWith("#"))
            {
                _logger?.LogWarning("Ignoring line: {Line}", lines[i]);
                continue;
            }
            var isUri = Uri.TryCreate(lines[i], UriKind.RelativeOrAbsolute, out var uri);
            if (!isUri || uri == null)
            {
                _logger?.LogWarning("Invalid URI: {Uri}", lines[i]);
                continue;
            }
            if (!File.Exists(uri.LocalPath))
            {
                _logger?.LogWarning("File does not exist: {Path}", uri.LocalPath);
                continue;
            }
            
            var mediaItem = await dataManager.GetMediaItemAsync(uri.LocalPath);
            if (mediaItem == null)
            {
                _logger?.LogWarning("Failed to get media item: {Path}", uri.LocalPath);
                continue;
            }

            await mediaItem.CacheAsync();
            var item = new PlaylistItemModel(playlist, mediaItem);
            playlist.PlaylistItems.Add(item);
        }
        return playlist;
    }
    
    public async ValueTask<bool> SavePlaylist(PlaylistModel playlist, string path)
    {
        if (string.IsNullOrWhiteSpace(playlist.Name))
        {
            _logger?.LogError("Playlist name is empty");
            return false;
        }
        
        await using var writer = new FileStream(path, FileMode.Create, FileAccess.Write);
        await using var stream = new StreamWriter(writer);
        await stream.WriteLineAsync("#EXTM3U");
        await stream.WriteLineAsync($"#EXTPLNAME:{playlist.Name}");
        foreach (var item in playlist.PlaylistItems)
        {
            await stream.WriteLineAsync(item.Item.SourcePath);
        }
        return true;
    }
    
    public async ValueTask<bool> SavePlaylistCollection(IEnumerable<PlaylistModel> playlists, string path)
    {
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var playlist in playlists)
        {
            var entry = zip.CreateEntry($"{playlist.Name}.m3u");
            await using var stream = entry.Open();
            await using var writer = new StreamWriter(stream);
            await writer.WriteLineAsync("#EXTM3U");
            await writer.WriteLineAsync($"#EXTPLNAME:{playlist.Name}");
            foreach (var item in playlist.PlaylistItems)
            {
                await writer.WriteLineAsync(item.Item.SourcePath);
            }
        }
        return true;
    }
}