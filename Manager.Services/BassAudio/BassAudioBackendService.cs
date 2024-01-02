using System.Runtime.InteropServices;
using ManagedBass;
using ManagedBass.Mix;
using Manager.Shared;
using Manager.Shared.Delegates;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces;

namespace Manager.Services.BassAudio;

public class BassAudioBackendService : ManagerComponent, IAdvancedAudioBackendService
{
    public event AudioServiceGlobalDeviceChangedEventHandler? GlobalDeviceChanged;
    public event BackendServiceChannelCreatedEventHandler? ChannelCreated;
    public event BackendServiceChannelDestroyedEventHandler? ChannelDestroyed;
    public event AudioServiceChannelVolumeChangedEventHandler? ChannelVolumeChanged;
    public event BackendServiceChannelStateChangedEventHandler? ChannelStateChanged;
    public event BackendServiceChannelPositionChangedEventHandler? ChannelPositionChanged;

    public BassAudioBackendService(string name, ulong parent) : base(name, parent)
    { }
    
    public override ValueTask<bool> InitializeAsync(params string[] options)
    {
        var freq = 44100;
        var device = -1;
        for (var i = 0; i < options.Length; i++)
        {
            if (options[i] == "-f" && i + 1 < options.Length && int.TryParse(options[i + 1], out var f))
            {
                freq = f;
                break;
            }
            if (options[i] == "-d" && i + 1 < options.Length && int.TryParse(options[i + 1], out var d))
            {
                device = d;
                break;
            }
        }
        //INFO: maybe check for options
        var bassInit = Bass.Init(device, freq);
        if (!bassInit)
        {
            var error = Bass.LastError;
            this.SendError(this, nameof(InitializeAsync), $"Failed to initialize BASS: {error}");
            return ValueTask.FromResult(false);
        }
        
        //load plugins
        var plugins = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Natives", "BassPlugins"));
        foreach (var plugin in plugins)
        {
            var pluginHandle = Bass.PluginLoad(plugin);
            if (pluginHandle != 0) 
                continue;
            var error = Bass.LastError;
            this.SendError(this, nameof(InitializeAsync), $"Failed to load BASS plugin: {plugin} - {error}");
            return ValueTask.FromResult(false);
        }
        return ValueTask.FromResult(true);
    }
    
    public ValueTask<AudioDevice[]?> GetDevicesAsync()
    {
        var devCount = Bass.DeviceCount;
        var devices = new AudioDevice[devCount];
        for (var i = 0; i < devCount; i++)
        {
            var deviceInfo = Bass.GetDeviceInfo(i);
            devices[i] = new AudioDevice
            {
                Id = i.ToString(),
                Name = deviceInfo.Name,
                AssociatedBackend = this
            };
        }
        return ValueTask.FromResult((AudioDevice[]?)devices);
    }

    public ValueTask<AudioDevice?> GetCurrentlySelectedDeviceAsync()
    {
        var device = Bass.CurrentDevice;
        var deviceInfo = Bass.GetDeviceInfo(device);
        return ValueTask.FromResult((AudioDevice?)new AudioDevice
        {
            Id = device.ToString(),
            Name = deviceInfo.Name,
            AssociatedBackend = this
        });
    }

    public ValueTask<bool> SetDeviceAsync(AudioDevice device)
    {
        if (device.AssociatedBackend != this)
        {
            this.SendError(this, nameof(SetDeviceAsync), $"Device {device.Name} is not associated with this backend");
            return ValueTask.FromResult(false);
        }
        
        try
        {
            var couldParse = int.TryParse(device.Id, out var id);
            if (!couldParse)
            {
                this.SendError(this, nameof(SetDeviceAsync), $"Failed to parse device id {device.Id}");
                return ValueTask.FromResult(false);
            }
            Bass.CurrentDevice = id;
        }
        catch (Exception e)
        {
            this.SendError(this, nameof(SetDeviceAsync), $"Failed to set device {device.Name}: {e.Message}");
            return ValueTask.FromResult(false);
        }
        this.GlobalDeviceChanged?.Invoke(this, device);
        return ValueTask.FromResult(true);
    }

    public ValueTask<AudioDevice?> GetChannelDeviceAsync(IMediaChannel channel)
    {
        if (channel is not BassAudioChannel bassChannel)
        {
            this.SendError(this, nameof(GetChannelDeviceAsync), $"Channel {channel.PlayItem.Title} is not a BassAudioChannel");
            return ValueTask.FromResult(default(AudioDevice?));
        }
        if (bassChannel.AssociatedAudioBackend != this)
        {
            this.SendError(this, nameof(GetChannelDeviceAsync), $"Channel {channel.PlayItem.Title} is not associated with this backend");
            return ValueTask.FromResult(default(AudioDevice?));
        }
        
        var device = Bass.ChannelGetDevice(bassChannel.MixerChannelHandle);
        var deviceInfo = Bass.GetDeviceInfo(device);
        return ValueTask.FromResult((AudioDevice?)new AudioDevice
        {
            Id = device.ToString(),
            Name = deviceInfo.Name,
            AssociatedBackend = this
        });
    }

    public ValueTask<bool> SetChannelDeviceAsync(IMediaChannel channel, AudioDevice device)
    {
        if (device.AssociatedBackend != this)
        {
            this.SendError(this, nameof(SetChannelDeviceAsync), $"Device {device.Name} is not associated with this backend");
            return ValueTask.FromResult(false);
        }
        if (channel is not BassAudioChannel bassChannel)
        {
            this.SendError(this, nameof(SetChannelDeviceAsync), $"Channel {channel.PlayItem.Title} is not a BassAudioChannel");
            return ValueTask.FromResult(false);
        }
        if (bassChannel.AssociatedAudioBackend != this)
        {
            this.SendError(this, nameof(SetChannelDeviceAsync), $"Channel {channel.PlayItem.Title} is not associated with this backend");
            return ValueTask.FromResult(false);
        }
        
        try
        {
            var couldParse = int.TryParse(device.Id, out var id);
            if (!couldParse)
            {
                this.SendError(this, nameof(SetChannelDeviceAsync), $"Failed to parse device id {device.Id}");
                return ValueTask.FromResult(false);
            }
            var couldSet = Bass.ChannelSetDevice(bassChannel.MixerChannelHandle, id);
            if (!couldSet)
            {
                var error = Bass.LastError;
                this.SendError(this, nameof(SetChannelDeviceAsync), $"Failed to set device {device.Name} for channel {channel.PlayItem.Title}: {error}");
                return ValueTask.FromResult(false);
            }
        }
        catch (Exception e)
        {
            this.SendError(this, nameof(SetChannelDeviceAsync), $"Failed to set device {device.Name} for channel {channel.PlayItem.Title}: {e.Message}");
            return ValueTask.FromResult(false);
        }
        return ValueTask.FromResult(true);
    }
    
    public ValueTask<IMediaChannel?> CreateChannelAsync(PlayItem playItem, Action<PlayItem>? onEnded = null)
        => this.CreateChannelAsync(playItem, 1f, 44100, null, onEnded);
    
    public async ValueTask<IMediaChannel?> CreateChannelAsync(PlayItem playItem, float volume = 1f, int frequency = 44100, AudioDevice? device = null, Action<PlayItem>? onEnded = null)
    {
        if (device != null && device.AssociatedBackend != this)
        {
            this.SendError(this, nameof(CreateChannelAsync), $"Device {device.Name} is not associated with this backend");
            return default;
        }

        if (playItem.CacheState == CacheState.NotCached)
        {
            this.SendError(this, nameof(CreateChannelAsync), $"PlayItem {playItem.Title} is not cached");
            return default;
        }

        int baseHandle;
        if (playItem.CacheState == CacheState.Memory)
        {
            var cachedStream = await playItem.CacheStrategy.GetCachedStreamAsync(playItem) as MemoryStream;
            if (cachedStream == null)
            {
                this.SendError(this, nameof(CreateChannelAsync), $"Failed to get cached stream for {playItem.Title}");
                return default;
            }
            baseHandle = Bass.CreateStream(cachedStream.GetBuffer(), 0, cachedStream.Length, BassFlags.Default | BassFlags.Float | BassFlags.Decode);
        }
        else
        {
            var cachedPath = await playItem.CacheStrategy.GetCachedPathAsync(playItem);
            if (cachedPath == null)
            {
                this.SendError(this, nameof(CreateChannelAsync), $"Failed to get cached path for {playItem.Title}");
                return default;
            }
            baseHandle = Bass.CreateStream(cachedPath, 0, 0, BassFlags.Default | BassFlags.Float | BassFlags.Decode);
        }
        

        if (baseHandle == 0)
        {
            var error = Bass.LastError;
            this.SendError(this, nameof(CreateChannelAsync), $"Failed to create channel for {playItem.Title}: {error}");
            return default;
        }
        
        //create mixerChannel for resampling
        var mixerHandle = BassMix.CreateMixerStream(frequency, 2, BassFlags.Default | BassFlags.Float | BassFlags.MixerEnd);
        if (mixerHandle == 0)
        {
            var error = Bass.LastError;
            this.SendError(this, nameof(CreateChannelAsync), $"Failed to create mixer channel for {playItem.Title}: {error}");
            Bass.StreamFree(baseHandle);
            return default;
        }
        
        //add stream to mixerChannel
        var couldAddStream = BassMix.MixerAddChannel(mixerHandle, baseHandle, BassFlags.Default | BassFlags.Float);
        if (!couldAddStream)
        {
            var error = Bass.LastError;
            this.SendError(this, nameof(CreateChannelAsync), $"Failed to add stream to mixer channel for {playItem.Title}: {error}");
            Bass.StreamFree(baseHandle);
            Bass.StreamFree(mixerHandle);
            return default;
        }
        
        var couldParse = int.TryParse(device?.Id, out var deviceId);
        if (!couldParse)
        {
            this.SendError(this, nameof(CreateChannelAsync), $"Failed to parse device id {device?.Id}");
            Bass.StreamFree(baseHandle);
            Bass.StreamFree(mixerHandle);
            return default;
        }
        
        var couldSetDevice = device == null || Bass.ChannelSetDevice(mixerHandle, deviceId);
        if (!couldSetDevice && Bass.LastError != Errors.Already)
        {
            var error = Bass.LastError;
            this.SendError(this, nameof(CreateChannelAsync), $"Failed to set device {device!.Name} for channel {playItem.Title}: {error}");
            Bass.StreamFree(baseHandle);
            Bass.StreamFree(mixerHandle);
            return default;
        }
        
        var couldSetVolume = Bass.ChannelSetAttribute(mixerHandle, ChannelAttribute.Volume, volume);
        if (!couldSetVolume)
        {
            var error = Bass.LastError;
            this.SendError(this, nameof(CreateChannelAsync), $"Failed to set volume {volume} for channel {playItem.Title}: {error}");
            Bass.StreamFree(baseHandle);
            Bass.StreamFree(mixerHandle);
            return default;
        }
        
        if (onEnded != null)
        {
            var couldSetCallback = Bass.ChannelSetSync(mixerHandle, SyncFlags.End, 0, 
                (_, _, _, _) => onEnded(playItem));
            if (couldSetCallback == 0)
            {
                var error = Bass.LastError;
                this.SendError(this, nameof(CreateChannelAsync), $"Failed to set callback for channel {playItem.Title}: {error}");
                Bass.StreamFree(baseHandle);
                Bass.StreamFree(mixerHandle);
                return default;
            }
        }
        
        var channel = new BassAudioChannel(this, playItem, baseHandle, mixerHandle);
        this.ChannelCreated?.Invoke(this, channel);
        return channel;
    }

    public ValueTask<bool> DestroyChannelAsync(IMediaChannel channel)
    {
        if (channel is not BassAudioChannel bassChannel)
        {
            this.SendError(this, nameof(DestroyChannelAsync), $"Channel {channel.PlayItem.Title} is not a BassAudioChannel");
            return ValueTask.FromResult(false);
        }
        if (bassChannel.AssociatedAudioBackend != this)
        {
            this.SendError(this, nameof(DestroyChannelAsync), $"Channel {channel.PlayItem.Title} is not associated with this backend");
            return ValueTask.FromResult(false);
        }
        
        var couldFreeMixer = Bass.StreamFree(bassChannel.MixerChannelHandle);
        if (!couldFreeMixer)
        {
            var error = Bass.LastError;
            this.SendError(this, nameof(DestroyChannelAsync), $"Failed to free mixer channel {channel.PlayItem.Title}: {error}");
            return ValueTask.FromResult(false);
        }
        
        var couldFreeBase = Bass.StreamFree(bassChannel.BaseChannelHandle);
        if (!couldFreeBase)
        {
            var error = Bass.LastError;
            this.SendError(this, nameof(DestroyChannelAsync), $"Failed to free base channel {channel.PlayItem.Title}: {error}");
            return ValueTask.FromResult(false);
        }
        
        this.ChannelDestroyed?.Invoke(this, channel);
        return ValueTask.FromResult(true);
    }

    public ValueTask<float?> GetChannelVolumeAsync(IMediaChannel channel)
    {
        if (channel is not BassAudioChannel bassChannel)
        {
            this.SendError(this, nameof(GetChannelVolumeAsync), $"Channel {channel.PlayItem.Title} is not a BassAudioChannel");
            return ValueTask.FromResult(default(float?));
        }
        if (bassChannel.AssociatedAudioBackend != this)
        {
            this.SendError(this, nameof(GetChannelVolumeAsync), $"Channel {channel.PlayItem.Title} is not associated with this backend");
            return ValueTask.FromResult(default(float?));
        }
        
        var couldGetVolume = Bass.ChannelGetAttribute(bassChannel.MixerChannelHandle, ChannelAttribute.Volume, out var volume);
        if (!couldGetVolume)
        {
            var error = Bass.LastError;
            this.SendError(this, nameof(GetChannelVolumeAsync), $"Failed to get volume for channel {channel.PlayItem.Title}: {error}");
            return ValueTask.FromResult(default(float?));
        }
        return ValueTask.FromResult((float?)volume);
    }

    public ValueTask<bool> SetChannelVolumeAsync(IMediaChannel channel, float volume)
    {
        if (channel is not BassAudioChannel bassChannel)
        {
            this.SendError(this, nameof(SetChannelVolumeAsync), $"Channel {channel.PlayItem.Title} is not a BassAudioChannel");
            return ValueTask.FromResult(false);
        }
        if (bassChannel.AssociatedAudioBackend != this)
        {
            this.SendError(this, nameof(SetChannelVolumeAsync), $"Channel {channel.PlayItem.Title} is not associated with this backend");
            return ValueTask.FromResult(false);
        }
        
        var couldSetVolume = Bass.ChannelSetAttribute(bassChannel.MixerChannelHandle, ChannelAttribute.Volume, volume);
        if (!couldSetVolume)
        {
            var error = Bass.LastError;
            this.SendError(this, nameof(SetChannelVolumeAsync), $"Failed to set volume {volume} for channel {channel.PlayItem.Title}: {error}");
            return ValueTask.FromResult(false);
        }
        this.ChannelVolumeChanged?.Invoke(this, channel, volume);
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> PlayChannelAsync(IMediaChannel channel)
    {
        if (channel is not BassAudioChannel bassChannel)
        {
            this.SendError(this, nameof(PlayChannelAsync), $"Channel {channel.PlayItem.Title} is not a BassAudioChannel");
            return ValueTask.FromResult(false);
        }
        if (bassChannel.AssociatedAudioBackend != this)
        {
            this.SendError(this, nameof(PlayChannelAsync), $"Channel {channel.PlayItem.Title} is not associated with this backend");
            return ValueTask.FromResult(false);
        }
        
        var couldPlay = Bass.ChannelPlay(bassChannel.MixerChannelHandle);
        if (!couldPlay)
        {
            var error = Bass.LastError;
            this.SendError(this, nameof(PlayChannelAsync), $"Failed to play channel {channel.PlayItem.Title}: {error}");
            return ValueTask.FromResult(false);
        }
        this.ChannelStateChanged?.Invoke(this, channel, ChannelState.Playing);
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> PauseChannelAsync(IMediaChannel channel)
    {
        if (channel is not BassAudioChannel bassChannel)
        {
            this.SendError(this, nameof(PauseChannelAsync), $"Channel {channel.PlayItem.Title} is not a BassAudioChannel");
            return ValueTask.FromResult(false);
        }
        if (bassChannel.AssociatedAudioBackend != this)
        {
            this.SendError(this, nameof(PauseChannelAsync), $"Channel {channel.PlayItem.Title} is not associated with this backend");
            return ValueTask.FromResult(false);
        }
        
        var couldPause = Bass.ChannelPause(bassChannel.MixerChannelHandle);
        if (!couldPause)
        {
            var error = Bass.LastError;
            this.SendError(this, nameof(PauseChannelAsync), $"Failed to pause channel {channel.PlayItem.Title}: {error}");
            return ValueTask.FromResult(false);
        }
        this.ChannelStateChanged?.Invoke(this, channel, ChannelState.Paused);
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> ResumeChannelAsync(IMediaChannel channel)
    {
        if (channel is not BassAudioChannel bassChannel)
        {
            this.SendError(this, nameof(ResumeChannelAsync), $"Channel {channel.PlayItem.Title} is not a BassAudioChannel");
            return ValueTask.FromResult(false);
        }
        if (bassChannel.AssociatedAudioBackend != this)
        {
            this.SendError(this, nameof(ResumeChannelAsync), $"Channel {channel.PlayItem.Title} is not associated with this backend");
            return ValueTask.FromResult(false);
        }
        
        var couldResume = Bass.ChannelPlay(bassChannel.MixerChannelHandle);
        if (!couldResume)
        {
            var error = Bass.LastError;
            this.SendError(this, nameof(ResumeChannelAsync), $"Failed to resume channel {channel.PlayItem.Title}: {error}");
            return ValueTask.FromResult(false);
        }
        this.ChannelStateChanged?.Invoke(this, channel, ChannelState.Playing);
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> StopChannelAsync(IMediaChannel channel)
    {
        if (channel is not BassAudioChannel bassChannel)
        {
            this.SendError(this, nameof(StopChannelAsync), $"Channel {channel.PlayItem.Title} is not a BassAudioChannel");
            return ValueTask.FromResult(false);
        }
        if (bassChannel.AssociatedAudioBackend != this)
        {
            this.SendError(this, nameof(StopChannelAsync), $"Channel {channel.PlayItem.Title} is not associated with this backend");
            return ValueTask.FromResult(false);
        }
        
        var couldStop = Bass.ChannelStop(bassChannel.MixerChannelHandle);
        if (!couldStop)
        {
            var error = Bass.LastError;
            this.SendError(this, nameof(StopChannelAsync), $"Failed to stop channel {channel.PlayItem.Title}: {error}");
            return ValueTask.FromResult(false);
        }
        this.ChannelStateChanged?.Invoke(this, channel, ChannelState.Stopped);
        return ValueTask.FromResult(true);
    }

    public ValueTask<ChannelState?> GetChannelStateAsync(IMediaChannel channel)
    {
        if (channel is not BassAudioChannel bassChannel)
        {
            this.SendError(this, nameof(GetChannelStateAsync), $"Channel {channel.PlayItem.Title} is not a BassAudioChannel");
            return ValueTask.FromResult(default(ChannelState?));
        }
        if (bassChannel.AssociatedAudioBackend != this)
        {
            this.SendError(this, nameof(GetChannelStateAsync), $"Channel {channel.PlayItem.Title} is not associated with this backend");
            return ValueTask.FromResult(default(ChannelState?));
        }
        
        var state = Bass.ChannelIsActive(bassChannel.MixerChannelHandle);
        return ValueTask.FromResult(state switch
        {
            PlaybackState.Playing => ChannelState.Playing,
            PlaybackState.Stopped => ChannelState.Stopped,
            PlaybackState.Paused => ChannelState.Paused,
            _ => default(ChannelState?)
        });
    }

    public ValueTask<bool> SetChannelStateAsync(IMediaChannel channel, ChannelState state)
    {
        return state switch
        {
            ChannelState.Playing => this.PlayChannelAsync(channel),
            ChannelState.Paused => this.PauseChannelAsync(channel),
            ChannelState.Stopped => this.StopChannelAsync(channel),
            _ => ValueTask.FromResult(false)
        };
    }

    public ValueTask<TimeSpan?> GetChannelPositionAsync(IMediaChannel channel)
    {
        if (channel is not BassAudioChannel bassChannel)
        {
            this.SendError(this, nameof(GetChannelPositionAsync), $"Channel {channel.PlayItem.Title} is not a BassAudioChannel");
            return ValueTask.FromResult(default(TimeSpan?));
        }
        if (bassChannel.AssociatedAudioBackend != this)
        {
            this.SendError(this, nameof(GetChannelPositionAsync), $"Channel {channel.PlayItem.Title} is not associated with this backend");
            return ValueTask.FromResult(default(TimeSpan?));
        }
        
        var positionBytes = Bass.ChannelGetPosition(bassChannel.MixerChannelHandle);
        var position = Bass.ChannelBytes2Seconds(bassChannel.MixerChannelHandle, positionBytes);
        return ValueTask.FromResult((TimeSpan?)TimeSpan.FromSeconds(position));
    }

    public ValueTask<bool> SetChannelPositionAsync(IMediaChannel channel, double positionMs)
    {
        if (channel is not BassAudioChannel bassChannel)
        {
            this.SendError(this, nameof(SetChannelPositionAsync), $"Channel {channel.PlayItem.Title} is not a BassAudioChannel");
            return ValueTask.FromResult(false);
        }
        if (bassChannel.AssociatedAudioBackend != this)
        {
            this.SendError(this, nameof(SetChannelPositionAsync), $"Channel {channel.PlayItem.Title} is not associated with this backend");
            return ValueTask.FromResult(false);
        }
        
        var positionBytes = Bass.ChannelSeconds2Bytes(bassChannel.MixerChannelHandle, positionMs / 1000);
        var couldSetPosition = Bass.ChannelSetPosition(bassChannel.MixerChannelHandle, positionBytes);
        if (!couldSetPosition)
        {
            var error = Bass.LastError;
            this.SendError(this, nameof(SetChannelPositionAsync), $"Failed to set position {positionMs}ms for channel {channel.PlayItem.Title}: {error}");
            return ValueTask.FromResult(false);
        }
        this.ChannelPositionChanged?.Invoke(this, channel, TimeSpan.FromMilliseconds(positionMs));
        return ValueTask.FromResult(true);
    }
    
    public ValueTask<bool> SetChannelPositionAsync(IMediaChannel channel, TimeSpan position)
        => this.SetChannelPositionAsync(channel, position.TotalMilliseconds);

    public ValueTask<TimeSpan?> GetChannelLengthAsync(IMediaChannel channel)
    {
        if (channel is not BassAudioChannel bassChannel)
        {
            this.SendError(this, nameof(GetChannelLengthAsync), $"Channel {channel.PlayItem.Title} is not a BassAudioChannel");
            return ValueTask.FromResult(default(TimeSpan?));
        }
        if (bassChannel.AssociatedAudioBackend != this)
        {
            this.SendError(this, nameof(GetChannelLengthAsync), $"Channel {channel.PlayItem.Title} is not associated with this backend");
            return ValueTask.FromResult(default(TimeSpan?));
        }
        
        var lengthBytes = Bass.ChannelGetLength(bassChannel.MixerChannelHandle);
        var length = Bass.ChannelBytes2Seconds(bassChannel.MixerChannelHandle, lengthBytes);
        return ValueTask.FromResult((TimeSpan?)TimeSpan.FromSeconds(length));
    }

    static BassAudioBackendService()
    {
        //Set resolver for BASS since its in the "Natives" folder
        NativeLibrary.SetDllImportResolver(typeof(Bass).Assembly,
            (name, _, _) =>
                NativeLibrary.Load(Path.Combine(Directory.GetCurrentDirectory(), "Natives", name)));
        NativeLibrary.SetDllImportResolver(typeof(BassMix).Assembly,
            (name, _, _) =>
                NativeLibrary.Load(Path.Combine(Directory.GetCurrentDirectory(), "Natives", name)));
    }
}