using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Manager.Shared;
using Manager.Shared.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimplePlayer.ViewModels;

namespace SimplePlayer.API;

public class MediaPlayerApi
{
    private readonly MainViewModel _playerViewModel;
    private readonly ILogger<MediaPlayerApi>? _logger;
    private readonly WebApplication _app;
    
    public MediaPlayerApi(ComponentManager componentManager, MainViewModel playerViewModel)
    {
        _playerViewModel = playerViewModel;
        _logger = componentManager.CreateLogger<MediaPlayerApi>();
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions()
        {
            ApplicationName = "SimplePlayer Media Player API",
        });

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiJsonSerializerContext.Default);
        });
        builder.Services.AddAntiforgery();

        _app = builder.Build();
        var mpApi = _app.MapGroup("/uiPlayer");
        mpApi.MapPost("/playSelected", PlaySelectedHandler);
        mpApi.MapPost("/playByIndex", PlayByIndexHandler);
        mpApi.MapPost("/playByIndexName", PlayByIndexNameHandler);
        mpApi.MapPost("/playByNameIndex", PlayByNameIndexHandler);
        mpApi.MapPost("/playByName", PlayByNameHandler);
        mpApi.MapPost("/playPath", PlayPathHandler);
        mpApi.MapPost("/pause", PauseHandler);
        mpApi.MapPost("/resume", ResumeHandler);
        mpApi.MapPost("/stop", StopHandler);
        mpApi.MapPost("/setPosition", SetPositionHandler);
        mpApi.MapGet("/getPosition",
            () => _playerViewModel.Player.ActiveMediaChannel == null
                ? Results.NotFound()
                : Results.Ok(_playerViewModel.Player.ActiveMediaChannel.Position));

        _app.UseAntiforgery();
    }

    private async Task PlaySelectedHandler(HttpContext context)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (_playerViewModel.SelectedPlaylistItem == null)
            {
                _logger?.LogError("No current item selected");
                await Results.NotFound("No current item selected").ExecuteAsync(context);
                return;
            }
            await _playerViewModel.Play();
        });
        await Results.Ok(true).ExecuteAsync(context);
    }

    public ValueTask<bool> StartApi()
    {
        try
        {
            _logger?.LogInformation("Starting API");
            _ = Task.Run(() => _app.RunAsync($"http://0.0.0.0:{_playerViewModel.ApiPort}"));
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to start API");
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> StopApi()
    {
        try
        {
            _logger?.LogInformation("Stopping API");
            _app.StopAsync();
            _app.DisposeAsync();
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to stop API");
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(true);
    }

    private async Task PlayPathHandler(HttpContext context)
    {
        var pathReq = await context.Request.ReadFromJsonAsync<PlayPathRequest>();
        if (pathReq == null || string.IsNullOrWhiteSpace(pathReq.Path))
        {
            _logger?.LogError("Failed to parse request");
            await Results.BadRequest("Failed to parse request").ExecuteAsync(context);
            return;
        }

        var path = pathReq.Path;
        //sanitize path, remove leading quotes and trailing quotes
        if (path.StartsWith("\"") && path.EndsWith("\""))
            path = path[1..^1];
        var scratchList = _playerViewModel.Playlists.FirstOrDefault(x => !x.IsRemovable);
        if (scratchList == null)
        {
            _logger?.LogError("No scratch list found???");
            await Results.Problem("No scratch list found").ExecuteAsync(context);
            return;
        }

        //check if track is already in the known playlists
        var track = _playerViewModel.Player.StoredMedia.FirstOrDefault(x =>
            x.SourcePath == path || x.PathTitle == path || x.PathTitle == Path.GetFileNameWithoutExtension(path));
        if (track == null)
        {
            var plItem = await _playerViewModel.AddFileToPlaylist(scratchList, path);
            if (plItem == null)
            {
                _logger?.LogError("Failed to add track to playlist");
                await Results.Problem("Failed to add track to playlist").ExecuteAsync(context);
                return;
            }

            track = plItem.Item;
        }

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            _playerViewModel.VideoPlayerVisible = track.ItemType is ItemType.Video or ItemType.Image;
            await _playerViewModel.Player.PlayAsync(track);
        });
        await Results.Ok(true).ExecuteAsync(context);
    }

    private async Task PlayByNameHandler(HttpContext context)
    {
        var request = await context.Request.ReadFromJsonAsync<PlayByNameRequest>();
        if (request == null)
        {
            _logger?.LogError("Failed to parse request");
            await Results.BadRequest("Failed to parse request").ExecuteAsync(context);
            return;
        }

        var playlist = _playerViewModel.Playlists.FirstOrDefault(x => x.Name == request.PlaylistName);
        if (playlist == null)
        {
            _logger?.LogError("Playlist not found");
            await Results.NotFound("Playlist not found").ExecuteAsync(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.TrackName))
        {
            _logger?.LogError("Track name is empty");
            await Results.BadRequest("Track name is empty").ExecuteAsync(context);
            return;
        }

        var track = playlist.PlaylistItems.FirstOrDefault(x =>
            x.Item.PathTitle.Contains(request.TrackName) ||
            x.Item.SourcePath == request.TrackName);
        if (track == null)
        {
            _logger?.LogError("Track not found in playlist: {playlist}", playlist.Name);
            await Results.NotFound("Track not found in playlist").ExecuteAsync(context);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            _playerViewModel.VideoPlayerVisible = track.Item.ItemType is ItemType.Video or ItemType.Image;
            _playerViewModel.CurrentItem = track;
            await _playerViewModel.Player.PlayAsync(track.Item);
        });
        _logger?.LogInformation("Playing track {track} from playlist {playlist}", track.Name, playlist.Name);
        await Results.Ok(true).ExecuteAsync(context);
    }

    private async Task PlayByNameIndexHandler(HttpContext context)
    {
        var request = await context.Request.ReadFromJsonAsync<PlayByNameIndexRequest>();
        if (request == null)
        {
            _logger?.LogError("Failed to parse request");
            await Results.BadRequest("Failed to parse request").ExecuteAsync(context);
            return;
        }

        var playlist = _playerViewModel.Playlists.FirstOrDefault(x => x.Name == request.PlaylistName);
        if (playlist == null)
        {
            _logger?.LogError("Playlist not found");
            await Results.NotFound("Playlist not found").ExecuteAsync(context);
            return;
        }

        var track = playlist.PlaylistItems.ElementAtOrDefault(request.TrackIndex);
        if (track == null)
        {
            _logger?.LogError("Track not found in playlist: {playlist}", playlist.Name);
            await Results.NotFound("Track not found in playlist").ExecuteAsync(context);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            _playerViewModel.VideoPlayerVisible = track.Item.ItemType is ItemType.Video or ItemType.Image;
            _playerViewModel.CurrentItem = track;
            await _playerViewModel.Player.PlayAsync(track.Item);
        });
        _logger?.LogInformation("Playing track {track} from playlist {playlist}", track.Name, playlist.Name);
        await Results.Ok(true).ExecuteAsync(context);
    }

    private async Task PlayByIndexNameHandler(HttpContext context)
    {
        var request = await context.Request.ReadFromJsonAsync<PlayByIndexNameRequest>();
        if (request == null)
        {
            _logger?.LogError("Failed to parse request");
            await Results.BadRequest("Failed to parse request").ExecuteAsync(context);
            return;
        }

        var playlist = _playerViewModel.Playlists.ElementAtOrDefault(request.PlaylistIndex);
        if (playlist == null)
        {
            _logger?.LogError("Playlist not found");
            await Results.NotFound("Playlist not found").ExecuteAsync(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.TrackName))
        {
            _logger?.LogError("Track name is empty");
            await Results.BadRequest("Track name is empty").ExecuteAsync(context);
            return;
        }

        var track = playlist.PlaylistItems.FirstOrDefault(x =>
            x.Item.PathTitle.Contains(request.TrackName) ||
            x.Item.SourcePath == request.TrackName);
        if (track == null)
        {
            _logger?.LogError("Track not found in playlist: {playlist}", playlist.Name);
            await Results.NotFound("Track not found in playlist").ExecuteAsync(context);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            _playerViewModel.VideoPlayerVisible = track.Item.ItemType is ItemType.Video or ItemType.Image;
            _playerViewModel.CurrentItem = track;
            await _playerViewModel.Player.PlayAsync(track.Item);
        });
        _logger?.LogInformation("Playing track {track} from playlist {playlist}", track.Name, playlist.Name);
        await Results.Ok(true).ExecuteAsync(context);
    }

    private async Task PlayByIndexHandler(HttpContext context)
    {
        var request = await context.Request.ReadFromJsonAsync<PlayByIndexRequest>();
        if (request == null)
        {
            _logger?.LogError("Failed to parse request");
            await Results.BadRequest("Failed to parse request").ExecuteAsync(context);
            return;
        }

        var playlist = _playerViewModel.Playlists.ElementAtOrDefault(request.PlaylistIndex);
        if (playlist == null)
        {
            _logger?.LogError("Playlist not found");
            await Results.NotFound("Playlist not found").ExecuteAsync(context);
            return;
        }

        var track = playlist.PlaylistItems.ElementAtOrDefault(request.TrackIndex);
        if (track == null)
        {
            _logger?.LogError("Track not found in playlist: {playlist}", playlist.Name);
            await Results.NotFound("Track not found in playlist").ExecuteAsync(context);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            _playerViewModel.VideoPlayerVisible = track.Item.ItemType is ItemType.Video or ItemType.Image;
            _playerViewModel.CurrentItem = track;
            await _playerViewModel.Player.PlayAsync(track.Item);
        });

        _logger?.LogInformation("Playing track {track} from playlist {playlist}", track.Name, playlist.Name);
        await Results.Ok(true).ExecuteAsync(context);
    }

    private async Task StopHandler(HttpContext context)
    {
        _logger?.LogInformation("Stop");
        await _playerViewModel.Stop();
        await Results.Ok(true).ExecuteAsync(context);
    }

    private async Task ResumeHandler(HttpContext context)
    {
        _logger?.LogInformation("Resume");
        await _playerViewModel.Player.ResumeAsync();
        await Results.Ok(true).ExecuteAsync(context);
    }

    private async Task PauseHandler(HttpContext context)
    {
        _logger?.LogInformation("Pause");
        await _playerViewModel.Player.PauseAsync();
        await Results.Ok(true).ExecuteAsync(context);
    }

    private async Task SetPositionHandler(HttpContext context)
    {
        var request = await context.Request.ReadFromJsonAsync<SetPositionRequest>();
        if (request == null)
        {
            _logger?.LogError("Failed to parse request");
            await Results.BadRequest("Failed to parse request").ExecuteAsync(context);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (_playerViewModel.Player.ActiveMediaChannel == null)
            {
                _logger?.LogError("No active media channel");
                await Results.NotFound("No active media channel").ExecuteAsync(context);
                return;
            }

            await _playerViewModel.Player.SetPositionAsync(TimeSpan.FromSeconds(request.Position));
        });
        await Results.Ok(true).ExecuteAsync(context);
    }
}