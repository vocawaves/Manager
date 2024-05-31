using LibVLCSharp.Shared;
using Manager.Shared;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.Audio;
using Manager.Shared.Events.General;
using Manager.Shared.Extensions;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.Audio;
using Manager.Shared.Interfaces.General;
using Manager.Shared.Interfaces.Video;
using Microsoft.Extensions.Logging;

namespace Manager.MediaBackends.LibVLCPlayer;

public class LibVLCBackend : IVideoBackendService, IAudioBackendService
{
    public event AsyncEventHandler<GlobalDefaultVolumeChangedEventArgs>? GlobalVolumeChanged;
    public event AsyncEventHandler<GlobalAudioDeviceChangedEventArgs>? GlobalDeviceChanged;

    #region IManagerComponent
    
    public event AsyncEventHandler? InitSuccess;
    public event AsyncEventHandler<InitFailedEventArgs>? InitFailed;
    public bool Initialized { get; private set; }
    public ComponentManager ComponentManager { get; }
    public string Name { get; }
    public ulong Parent { get; }

    #endregion
    
    private LibVLC? _internalLibVLC;
    private MediaPlayer? _internalMediaPlayer;
    
    private AudioDevice? _currentDefaultDevice;
    private float _currentDefaultVolume = 1.0f;
    
    private readonly ILogger<LibVLCBackend>? _logger;

    private LibVLCBackend(ComponentManager componentManager, string name, ulong parent)
    {
        this.ComponentManager = componentManager;
        this.Name = name;
        this.Parent = parent;
        this._logger = componentManager.CreateLogger<LibVLCBackend>();
    }
    
    public static IManagerComponent? Create(ComponentManager componentManager, string name, ulong parent)
    {
        return new LibVLCBackend(componentManager, name, parent);
    }

    public ValueTask<bool> InitializeAsync(params string[] options)
    {
        if (this.Initialized)
            return ValueTask.FromResult(true);
        try
        {
            Core.Initialize();
            _internalLibVLC = new LibVLC();
            _internalMediaPlayer = new MediaPlayer(_internalLibVLC);
        }
        catch (Exception e)
        {
            this.InitFailed?.InvokeAndForget(this, new InitFailedEventArgs(e));
            this._logger?.LogError(e, "Failed to initialize LibVLC");
            return ValueTask.FromResult(false);
        }
        this.Initialized = true;
        this.InitSuccess?.InvokeAndForget(this, EventArgs.Empty);
        return ValueTask.FromResult(true);
    }
   
    public async ValueTask<IMediaChannel?> CreateChannelAsync(MediaItem item)
    {
        if (!this.Initialized || this._internalLibVLC == null)
        {
            this._logger?.LogError("LibVLC is not initialized");
            return null;
        }

        if (item.CacheState != CacheState.Cached)
        {
            this._logger?.LogError("Media item is not cached");
            return null;
        }
        
        if (item.ItemType is ItemType.Misc)
        {
            this._logger?.LogWarning("Media item type may not be supported, proceed with caution");
        }

        var path = await item.GetCachedPathAsync();
        if (string.IsNullOrEmpty(path))
        {
            this._logger?.LogError("Failed to get cached path for media item");
            return null;
        }
        
        var media = new Media(_internalLibVLC, path);
        var channelPlayer = new MediaPlayer(media);
        if (channelPlayer.Media == null)
        {
            this._logger?.LogError("Failed to create media player with media item");
            return null;
        }
        var couldParse = await channelPlayer.Media.Parse();
        if (couldParse != MediaParsedStatus.Done)
        {
            this._logger?.LogError("Failed to parse media item");
            return null;
        }
        return new LibVLCChannel(this, item, channelPlayer, this.ComponentManager.CreateLogger<LibVLCChannel>());
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