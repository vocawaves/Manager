using ManagedBass;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.Audio;
using Manager.Shared.Events.General;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.Audio;
using Microsoft.Extensions.Logging;

namespace Manager.BassPlayer;

public class BassBackend : IAudioBackendService
{
    public event AsyncEventHandler<InitSuccessEventArgs>? InitSuccess;
    public event AsyncEventHandler<InitFailedEventArgs>? InitFailed;

    public event AsyncEventHandler<GlobalDefaultVolumeChangedEventArgs>? GlobalVolumeChanged;
    public event AsyncEventHandler<GlobalAudioDeviceChangedEventArgs>? GlobalDeviceChanged;

    public bool Initialized { get; private set; }
    public string Name { get; }
    public ulong Parent { get; }

    private readonly ILogger<BassBackend>? _logger;
    private readonly ILoggerFactory? _logFactory;

    public BassBackend(string name, ulong parent, ILoggerFactory? lf = null)
    {
        this._logFactory = lf;
        this._logger = lf?.CreateLogger<BassBackend>();
        this.Name = name;
        this.Parent = parent;
    }

    public ValueTask<bool> InitializeAsync(params string[] options)
    {
        var bassInit = Bass.Init();
        if (!bassInit)
        {
            this.InitFailed?.InvokeAndForget(this,
                new InitFailedEventArgs(Bass.LastError.ToString()));
            this._logger?.LogError("Failed to initialize Bass: {BassLastError}", Bass.LastError);
            return ValueTask.FromResult(false);
        }

        this._logger?.LogInformation("Bass v{version} initialized", Bass.Version);

        var plugins = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "BassPlugins"));
        foreach (var plugin in plugins)
        {
            try
            {
                var index = Bass.PluginLoad(plugin);
                if (index == 0)
                    this._logger?.LogWarning("Failed to load plugin {plugin}: {BassLastError}", plugin, Bass.LastError);
                else
                    this._logger?.LogInformation("Loaded plugin {plugin} at index {index}", plugin, index);
            }
            catch (Exception e)
            {
                this._logger?.LogError(e, "Failed to load plugin {plugin}, {BassLastError}", plugin, Bass.LastError);
            }
        }

        this.Initialized = true;
        this.InitSuccess?.InvokeAndForget(this, new InitSuccessEventArgs($"Bass v{Bass.Version} initialized"));
        this._logger?.LogInformation("BassBackend initialized");
        return ValueTask.FromResult(true);
    }

    public ValueTask<AudioDevice[]> GetDevicesAsync()
    {
        var devices = new AudioDevice[Bass.DeviceCount];
        this._logger?.LogDebug("Found {count} devices", devices.Length);
        for (var i = 0; i < Bass.DeviceCount; i++)
        {
            var device = Bass.GetDeviceInfo(i);
            devices[i] = new AudioDevice(this, device.Name, i.ToString());
            this._logger?.LogDebug("Device {i}: {device}", i, devices[i]);
        }

        return ValueTask.FromResult(devices);
    }

    public ValueTask<AudioDevice> GetCurrentDeviceAsync()
    {
        var device = Bass.CurrentDevice;
        var ad = new AudioDevice(this, device == -1 ? "Default" : Bass.GetDeviceInfo(device).Name, device.ToString());
        this._logger?.LogDebug("Current device: {device}", ad);
        return ValueTask.FromResult(ad);
    }

    public async ValueTask<bool> SetDeviceAsync(AudioDevice device)
    {
        var validId = int.TryParse(device.Id, out var id);
        if (!validId)
        {
            this._logger?.LogError("Failed to parse device id from {device}", device);
            return false;
        }

        try
        {
            this._logger?.LogDebug("Setting device to {device}", device);
            var success = Bass.CurrentDevice = id;
            if (success == -1)
                this._logger?.LogError("Failed to set device to {device}: {BassLastError}", device, Bass.LastError);
            else
            {
                this.GlobalDeviceChanged?.InvokeAndForget(this,
                    new GlobalAudioDeviceChangedEventArgs(device));
                this._logger?.LogInformation("Set device to {device}", device);
            }

            return success != -1;
        }
        catch (Exception e)
        {
            var bassError = Bass.LastError;
            if (bassError == Errors.Init) //Device has not been initialized
            {
                this._logger?.LogDebug("Device has not been initialized, initializing device {device}", device);
                var bassInit = Bass.Init(id);
                if (bassInit)
                    return await this.SetDeviceAsync(device); //try again

                this._logger?.LogError("Failed to initialize new device {device}: {BassLastError}", device,
                    Bass.LastError);
                return false;
            }

            this._logger?.LogError(e, "Failed to set device to {device}: {BassLastError}", device, Bass.LastError);
            return false;
        }
    }

    public ValueTask<float> GetDefaultVolumeAsync()
    {
        var volume = Bass.Volume;
        this._logger?.LogDebug("Default volume: {volume}", volume);
        return ValueTask.FromResult((float)volume);
    }

    public ValueTask<bool> SetDefaultVolumeAsync(float volume)
    {
        try
        {
            var success = Bass.Volume = volume;
            if ((int)success == -1)
                this._logger?.LogError("Failed to set default volume to {volume}: {BassLastError}", volume,
                    Bass.LastError);
            else
            {
                this.GlobalVolumeChanged?.InvokeAndForget(this,
                    new GlobalDefaultVolumeChangedEventArgs(volume));
                this._logger?.LogInformation("Set default volume to {volume}", volume);
            }

            return ValueTask.FromResult(success >= 0);
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to set default volume to {volume}: {BassLastError}", volume,
                Bass.LastError);
            return ValueTask.FromResult(false);
        }
    }

    public async ValueTask<IAudioChannel?> CreateChannelAsync(AudioItem mediaItem)
    {
        if (!this.Initialized)
        {
            this._logger?.LogError("BassBackend has not been initialized");
            return default;
        }

        int stream;
        if (mediaItem.CacheState != CacheState.Cached)
        {
            this._logger?.LogError("MediaItem has not been cached");
            return default;
        }

        try
        {
            var cachePath = await mediaItem.GetCachePathAsync();
            switch (cachePath)
            {
                case null:
                    this._logger?.LogError("Failed to get cached stream for {mediaItem}", mediaItem);
                    return default;
                default:
                {
                    this._logger?.LogDebug("Using temporary MemoryStream for {mediaItem}", mediaItem);
                    stream = Bass.CreateStream(cachePath, 0, 0,
                        BassFlags.Default | BassFlags.Float);
                    break;
                }
            }
        }
        catch (Exception e)
        {
            this._logger?.LogError(e, "Failed to create stream for {audioItem}", mediaItem);
            return default;
        }

        if (stream == 0)
        {
            this._logger?.LogError("Failed to create stream for {audioItem}: {BassLastError}", mediaItem,
                Bass.LastError);
            return default;
        }

        var channel = new BassChannel(this, mediaItem, stream, this._logFactory?.CreateLogger<BassChannel>());
        this._logger?.LogInformation("Created channel for {SourcePath}", mediaItem.SourcePath);
        return channel;
    }

    public ValueTask<bool> IsMediaItemSupportedAsync(MediaItem mediaItem)
    {
        throw new NotImplementedException();
    }
}