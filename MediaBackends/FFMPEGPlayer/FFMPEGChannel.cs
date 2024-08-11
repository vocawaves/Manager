using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.General;
using Manager.Shared.Events.Video;
using Manager.Shared.Extensions;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.General;
using Manager.Shared.Interfaces.Video;
using Microsoft.Extensions.Logging;

namespace FFMPEGPlayer;

public class FFMPEGChannel : IVideoChannel
{
    public event AsyncEventHandler? Playing;
    public event AsyncEventHandler? Paused;
    public event AsyncEventHandler? Resumed;
    public event AsyncEventHandler? Stopped;
    public event AsyncEventHandler? Ended;
    public event AsyncEventHandler<ChannelStateChangedEventArgs>? StateChanged;
    public event AsyncEventHandler<ChannelPositionChangedEventArgs>? PositionChanged;
    public event AsyncEventHandler<ChannelPositionTriggerInvokedEventArgs>? PositionTriggerInvoked;
    public IBackendService AssociatedBackend { get; }
    public MediaItem MediaItem { get; }
    public List<PositionTrigger> PositionTriggers { get; } = new();
    public TimeSpan? Position { get; } = TimeSpan.Zero;
    public TimeSpan? Length { get; } = TimeSpan.Zero;
    
    public VideoDecoder? Decoder { get; set; }
    
    private readonly ILogger<FFMPEGChannel>? _logger;

    public FFMPEGChannel(IBackendService backend, MediaItem mediaItem, ILogger<FFMPEGChannel>? logger = null)
    {
        this.AssociatedBackend = backend;
        this.MediaItem = mediaItem;
        this._logger = logger ?? backend.ComponentManager.CreateLogger<FFMPEGChannel>();
    }
    
    public async ValueTask<bool> PrepareAsync()
    {
        var cachePath = await this.MediaItem.GetCachedPathAsync();
        if (string.IsNullOrEmpty(cachePath))
        {
            this._logger?.LogError("Failed to get cached path for media item");
            return false;
        }
        
        var decoderLogger = this.AssociatedBackend.ComponentManager.CreateLogger<VideoDecoder>();
        this.Decoder = new VideoDecoder(decoderLogger);
        var couldInitialize = this.Decoder.InitializeFromFile(cachePath);
        if (couldInitialize) 
            return true;
        this._logger?.LogError("Failed to initialize video decoder");
        return false;
    }
    
    public ValueTask<bool> PlayAsync()
    {
        this.Decoder?.Start();
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> PauseAsync()
    {
        this.Decoder?.Stop();
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> ResumeAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> StopAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<ChannelState> GetStateAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetStateAsync(ChannelState state)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TimeSpan?> GetPositionAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetPositionAsync(double positionMs)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetPositionAsync(TimeSpan position)
    {
        throw new NotImplementedException();
    }

    public ValueTask<PositionTrigger?> AddPositionTriggerAsync(string name, TimeSpan position)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> RemovePositionTriggerAsync(PositionTrigger trigger)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TimeSpan?> GetLengthAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> DestroyAsync()
    {
        throw new NotImplementedException();
    }
    
    public ValueTask DisposeAsync()
    {
        // TODO release managed resources here
        return ValueTask.CompletedTask;
    }

    public event AsyncEventHandler<ExternalPlayerSurfaceChangedEventArgs>? ExternalVideoSurfaceChanged;
    public IExternalPlayerSurface? ExternalVideoSurface { get; } = null;
    public ValueTask<bool> SetExternalVideoSurfaceAsync(IExternalPlayerSurface surface)
    {
        throw new NotImplementedException();
    }
}