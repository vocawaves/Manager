using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Manager.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimplePlayer.ViewModels;

namespace SimplePlayer.API;

public class SfxApi
{
    private readonly ILogger<SfxApi>? _logger;
    private readonly WebApplication _app;
    
    private readonly ComponentManager _componentManager;
    private readonly SoundBoardsViewModel _soundBoardsVm;

    public SfxApi(ComponentManager componentManager, SoundBoardsViewModel soundBoardsVm)
    {
        _componentManager = componentManager;
        _soundBoardsVm = soundBoardsVm;
        _logger = componentManager.CreateLogger<SfxApi>();
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions()
        {
            ApplicationName = "SimplePlayer SFX Player API",
        });

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiJsonSerializerContext.Default);
        });
        builder.Services.AddAntiforgery();

        _app = builder.Build();
        var mpApi = _app.MapGroup("/sfxPlayer");
        mpApi.MapPost("/playByIndex", PlayByIndexHandler);
        mpApi.MapPost("/playByGridIndex", PlayByGridIndexHandler);
        mpApi.MapPost("/stopAll", StopHandler);
        mpApi.MapPost("/stopAll/{index}", StopBoardHandler);
        mpApi.MapPost("/fadeAll/{index}", FadeBoardHandler);
        mpApi.MapPost("/fadeByIndex", FadeByIndexHandler);
        _app.UseAntiforgery();
    }

    private async Task FadeByIndexHandler(HttpContext context)
    {
        var request = await context.Request.ReadFromJsonAsync<SfxByIndexRequest>();
        if (request is null)
        {
            await Results.BadRequest("Invalid request").ExecuteAsync(context);
            return;
        }
        
        if (request.BoardIndex < 0 || request.BoardIndex >= _soundBoardsVm.SoundBoards.Count)
        {
            await Results.BadRequest("Invalid sound board index").ExecuteAsync(context);
            return;
        }
        
        var board = _soundBoardsVm.SoundBoards[request.BoardIndex];
        if (request.ButtonIndex < 0 || request.ButtonIndex >= board.Sounds.Count)
        {
            await Results.BadRequest("Invalid sound index").ExecuteAsync(context);
            return;
        }
        
        var sound = board.Sounds.FirstOrDefault(x => x.VisualIndex == request.ButtonIndex);
        if (sound is null)
        {
            await Results.BadRequest("Invalid sound index").ExecuteAsync(context);
            return;
        }
        
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                if (sound is not { IsPlaying: true, Channel: not null }) 
                    return;
                if (!sound.Fade)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        sound.Fade = true;
                        sound.Channel.Ended += ResetFadeWhenComplete;   
                        sound.Channel.Stopped += ResetFadeWhenComplete;
                    });
                }
                await sound.Trigger();
        
                async ValueTask ResetFadeWhenComplete(object sender, EventArgs e)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        sound.Fade = false;
                        sound.Channel.Ended -= ResetFadeWhenComplete;
                        sound.Channel.Stopped -= ResetFadeWhenComplete;
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }

    private async Task FadeBoardHandler(HttpContext context)
    {
        var get = context.Request.RouteValues["index"];
        if (get is null)
        {
            await Results.BadRequest("Invalid request").ExecuteAsync(context);
            return;
        }
        var isInt = int.TryParse(get.ToString(), out var index);
        if (!isInt)
        {
            await Results.BadRequest("Invalid request").ExecuteAsync(context);
            return;
        }
        
        if (index < 0 || index >= _soundBoardsVm.SoundBoards.Count)
        {
            await Results.BadRequest("Invalid sound board index").ExecuteAsync(context);
            return;
        }
        
        var board = _soundBoardsVm.SoundBoards[index];
        foreach (var sound in board.Sounds)
        {
            try
            {
                if (sound is not { IsPlaying: true, Channel: not null }) 
                    continue;
                if (!sound.Fade)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        sound.Fade = true;
                        sound.Channel.Ended += ResetFadeWhenComplete;   
                        sound.Channel.Stopped += ResetFadeWhenComplete; 
                    });
                }
                await sound.Trigger();
        
                async ValueTask ResetFadeWhenComplete(object sender, EventArgs e)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        sound.Fade = false;
                        sound.Channel.Ended -= ResetFadeWhenComplete;
                        sound.Channel.Stopped -= ResetFadeWhenComplete;
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        await Results.Ok().ExecuteAsync(context);
    }

    private async Task StopBoardHandler(HttpContext context)
    {
        var get = context.Request.RouteValues["index"];
        if (get is null)
        {
            await Results.BadRequest("Invalid request").ExecuteAsync(context);
            return;
        }
        var isInt = int.TryParse(get.ToString(), out var index);
        if (!isInt)
        {
            await Results.BadRequest("Invalid request").ExecuteAsync(context);
            return;
        }
        
        if (index < 0 || index >= _soundBoardsVm.SoundBoards.Count)
        {
            await Results.BadRequest("Invalid sound board index").ExecuteAsync(context);
            return;
        }
        
        var board = _soundBoardsVm.SoundBoards[index];
        foreach (var sound in board.Sounds)
        {
            try
            {
                if (sound is { IsPlaying: true, Channel: not null })
                    await sound.Channel.StopAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    private async Task PlayByGridIndexHandler(HttpContext context)
    {
        var request = await context.Request.ReadFromJsonAsync<SfxByGridIndexRequest>();
        if (request is null)
        {
            await Results.BadRequest("Invalid request").ExecuteAsync(context);
            return;
        }
        
        if (request.BoardIndex < 0 || request.BoardIndex >= _soundBoardsVm.SoundBoards.Count)
        {
            await Results.BadRequest("Invalid sound board index").ExecuteAsync(context);
            return;
        }
        
        var board = _soundBoardsVm.SoundBoards[request.BoardIndex];
        if (request.ButtonRow < 0 || request.ButtonRow >= board.BoardRows)
        {
            await Results.BadRequest("Invalid sound row index").ExecuteAsync(context);
            return;
        }
        
        if (request.ButtonColum < 0 || request.ButtonColum >= board.BoardColumns)
        {
            await Results.BadRequest("Invalid sound column index").ExecuteAsync(context);
            return;
        }
        
        var sound = board.Sounds.FirstOrDefault(x => x.Row == request.ButtonRow && x.Column == request.ButtonColum);
        if (sound is null)
        {
            await Results.BadRequest("Invalid sound index").ExecuteAsync(context);
            return;
        }
        
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                await sound.Trigger();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }

    private async Task StopHandler(HttpContext context)
    {
        foreach (var soundBoard in _soundBoardsVm.SoundBoards)
        {
            foreach (var sound in soundBoard.Sounds)
            {
                try
                {
                    if (sound.IsPlaying && sound.Channel != null)
                        await sound.Channel.StopAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        await Results.Ok().ExecuteAsync(context);
    }

    private async Task PlayByIndexHandler(HttpContext context)
    {
        var request = await context.Request.ReadFromJsonAsync<SfxByIndexRequest>();
        if (request is null)
        {
            await Results.BadRequest("Invalid request").ExecuteAsync(context);
            return;
        }
        
        if (request.BoardIndex < 0 || request.BoardIndex >= _soundBoardsVm.SoundBoards.Count)
        {
            await Results.BadRequest("Invalid sound board index").ExecuteAsync(context);
            return;
        }
        
        var board = _soundBoardsVm.SoundBoards[request.BoardIndex];
        if (request.ButtonIndex < 0 || request.ButtonIndex >= board.Sounds.Count)
        {
            await Results.BadRequest("Invalid sound index").ExecuteAsync(context);
            return;
        }
        
        var sound = board.Sounds.FirstOrDefault(x => x.VisualIndex == request.ButtonIndex);
        if (sound is null)
        {
            await Results.BadRequest("Invalid sound index").ExecuteAsync(context);
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                await sound.Trigger();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
        await Results.Ok().ExecuteAsync(context);
    }


    public ValueTask<bool> StartApi(int port)
    {
        try
        {
            _logger?.LogInformation("Starting SFX API");
            _ = Task.Run(() => _app.RunAsync($"http://0.0.0.0:{port}"));
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to start SFX API");
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> StopApi()
    {
        try
        {
            _logger?.LogInformation("Stopping SFX API");
            _app.StopAsync();
            _app.DisposeAsync();
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to stop SFX API");
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(true);
    }
}