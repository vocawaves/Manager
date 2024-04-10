using LibVLCSharp.Shared;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.Audio;
using Manager.Shared.Events.General;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.Audio;
using Manager.Shared.Interfaces.Video;
using Microsoft.Extensions.Logging;

namespace Manager.LibVLCPlayer;

public class LibVLCBackend : IVideoBackendService, IAudioBackendService
{
    public event AsyncEventHandler<InitSuccessEventArgs>? InitSuccess;
    public event AsyncEventHandler<InitFailedEventArgs>? InitFailed;

    public event AsyncEventHandler<GlobalDefaultVolumeChangedEventArgs>? GlobalVolumeChanged;
    public event AsyncEventHandler<GlobalAudioDeviceChangedEventArgs>? GlobalDeviceChanged;

    public bool Initialized { get; private set; }
    public string Name { get; }
    public ulong Parent { get; }
    
    private readonly ILogger<LibVLCBackend>? _logger;
    
    private LibVLC? _internalLibVLC;
    private MediaPlayer? _internalMediaPlayer;
    
    private AudioDevice? _currentDefaultDevice;
    private float _currentDefaultVolume = 1.0f;

    public LibVLCBackend(string name, ulong parent, LoggerFactory? lf = null)
    {
        this.Name = name;
        this.Parent = parent;
        this._logger = lf?.CreateLogger<LibVLCBackend>();
    }

    public ValueTask<bool> InitializeAsync(params string[] options)
    {
        try
        {
            Core.Initialize();
            _internalLibVLC = new LibVLC();
            _internalMediaPlayer = new MediaPlayer(_internalLibVLC);
        }
        catch (Exception e)
        {
            this.InitFailed?.InvokeAndForget(this,
                new InitFailedEventArgs(e.Message));
            this._logger?.LogError(e, "Failed to initialize LibVLC");
            return ValueTask.FromResult(false);
        }
        this.Initialized = true;
        this.InitSuccess?.InvokeAndForget(this, new InitSuccessEventArgs("LibVLC initialized"));
        return ValueTask.FromResult(true);
    }

    public ValueTask<IAudioChannel?> CreateChannelAsync(AudioItem item)
    {
        throw new NotImplementedException();
    }
    
    public async ValueTask<IVideoChannel?> CreateChannelAsync(VideoItem item)
    {
        if (!this.Initialized || this._internalMediaPlayer == null)
            throw new InvalidOperationException("LibVLC is not initialized");
        
        if (item.CacheState != CacheState.Cached)
            throw new InvalidOperationException("Video item is not cached");
        
        var cachePath = await item.GetCachePathAsync();
        if (cachePath == null)
            throw new Exception("Failed to get cache path for video item");

        var channelLibVLC = new LibVLC();
        var channelMediaPlayer = new MediaPlayer(channelLibVLC);
        var media = new Media(channelLibVLC, cachePath);
        var parseState = await media.Parse();
        if (parseState != MediaParsedStatus.Done)
            throw new Exception("Failed to parse media");

        channelMediaPlayer.Media = media;
        var channel = new LibVLCChannel(this, item, channelLibVLC, channelMediaPlayer, media);
        return channel;
    }

    public ValueTask<AudioDevice[]> GetDevicesAsync()
    {
        if (!this.Initialized || this._internalMediaPlayer == null)
            throw new InvalidOperationException("LibVLC is not initialized");
        var devices = this._internalMediaPlayer.AudioOutputDeviceEnum;
        this._logger?.LogDebug("Found {Count} audio devices", devices.Length);
        var result = new AudioDevice[devices.Length];
        for (var i = 0; i < devices.Length; i++)
        {
            var device = devices[i];
            this._logger?.LogDebug("Found audio device {Name} with ID {ID}", device.Description, device.DeviceIdentifier);
            result[i] = new AudioDevice(this, device.Description, device.DeviceIdentifier);
        }
        return ValueTask.FromResult(result);
    }

    public ValueTask<AudioDevice> GetCurrentDeviceAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetDeviceAsync(AudioDevice device)
    {
        throw new NotImplementedException();
    }

    public ValueTask<float> GetDefaultVolumeAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetDefaultVolumeAsync(float volume)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> IsMediaItemSupportedAsync(MediaItem mediaItem)
    {
        throw new NotImplementedException();
    }
}