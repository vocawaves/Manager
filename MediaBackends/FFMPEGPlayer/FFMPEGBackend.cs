using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using Manager.Shared;
using Manager.Shared.Entities;
using Manager.Shared.Events.General;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.General;
using Manager.Shared.Interfaces.Video;
using Microsoft.Extensions.Logging;

namespace FFMPEGPlayer;

public class FFMPEGBackend : IVideoBackendService, INeedsInitialization
{
    public event AsyncEventHandler? InitSuccess;
    public event AsyncEventHandler<InitFailedEventArgs>? InitFailed;

    public ComponentManager ComponentManager { get; }

    private readonly ILogger<FFMPEGBackend>? _logger;

    public string Name { get; }
    public ulong Parent { get; }
    public bool Initialized { get; private set; } = false;

    public FFMPEGBackend(ComponentManager componentManager, string name, ulong parent)
    {
        this.ComponentManager = componentManager;
        this.Name = name;
        this.Parent = parent;
        this._logger = componentManager.CreateLogger<FFMPEGBackend>();
    }

    public ValueTask<bool> InitializeAsync(params string[] options)
    {
        try
        {
#if DEBUG   
            //runtimes/win-x64/native
            ffmpeg.RootPath = Path.Combine(Directory.GetCurrentDirectory(), "runtimes", "win-x64", "native");
#endif
            var version = ffmpeg.av_version_info();
            _logger?.LogInformation($"FFMPEG version: {version}");
            ffmpeg.avdevice_register_all();
            _logger?.LogDebug("Registered all devices");
            ffmpeg.avformat_network_init();
            _logger?.LogDebug("Initialized network");
#if DEBUG
            ffmpeg.av_log_set_level(ffmpeg.AV_LOG_DEBUG);
#else
            ffmpeg.av_log_set_level(ffmpeg.AV_LOG_QUIET);
#endif
            unsafe
            {
                
            }
            _logger?.LogDebug("Set log callback");
            this.Initialized = true;
            this.InitSuccess?.InvokeAndForget(this, EventArgs.Empty);
            return ValueTask.FromResult(true);
        }
        catch (Exception e)
        {
            this.InitFailed?.InvokeAndForget(this, new InitFailedEventArgs(e));
            this._logger?.LogError(e, "Failed to initialize FFMPEG backend");
            return ValueTask.FromResult(false);
        }
    }

    public ValueTask<bool> IsMediaItemSupportedAsync(MediaItem mediaItem)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IMediaChannel?> CreateChannelAsync(MediaItem item)
    {
        throw new NotImplementedException();
    }
}