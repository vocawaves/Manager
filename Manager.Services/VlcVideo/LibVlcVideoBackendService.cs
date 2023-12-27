using LibVLCSharp.Shared;
using Manager.Shared;
using Manager.Shared.Delegates;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces;

namespace Manager.Services.VlcVideo;

public class LibVlcVideoBackendService : ManagerComponent, IVideoBackendService, IAudioBackendService
{
    public event AudioServiceGlobalDeviceChangedEventHandler? GlobalDeviceChanged;
    public event AudioServiceChannelVolumeChangedEventHandler? ChannelVolumeChanged;
    
    public event BackendServiceChannelCreatedEventHandler? ChannelCreated;
    public event BackendServiceChannelDestroyedEventHandler? ChannelDestroyed;
    public event BackendServiceChannelStateChangedEventHandler? ChannelStateChanged;
    public event BackendServiceChannelPositionChangedEventHandler? ChannelPositionChanged;
    
    public LibVlcVideoBackendService(string name, ulong parent) : base(name, parent)
    {
    }
    
    public ValueTask<IMediaChannel?> CreateChannelAsync(PlayItem playItem, Action<PlayItem>? onEnded = null)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> DestroyChannelAsync(IMediaChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> PlayChannelAsync(IMediaChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> PauseChannelAsync(IMediaChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> ResumeChannelAsync(IMediaChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> StopChannelAsync(IMediaChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ChannelState?> GetChannelStateAsync(IMediaChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetChannelStateAsync(IMediaChannel channel, ChannelState state)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TimeSpan?> GetChannelPositionAsync(IMediaChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetChannelPositionAsync(IMediaChannel channel, double positionMs)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetChannelPositionAsync(IMediaChannel channel, TimeSpan position)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TimeSpan?> GetChannelLengthAsync(IMediaChannel channel)
    {
        throw new NotImplementedException();
    }
    
    public ValueTask<AudioDevice[]> GetDevicesAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<AudioDevice> GetCurrentlySelectedDeviceAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetDeviceAsync(AudioDevice device)
    {
        throw new NotImplementedException();
    }

    public ValueTask<AudioDevice?> GetChannelDeviceAsync(IMediaChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetChannelDeviceAsync(IMediaChannel channel, AudioDevice device)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IMediaChannel?> CreateChannelAsync(PlayItem playItem, float volume = 1, int frequency = 44100, AudioDevice? device = null,
        Action<PlayItem>? onEnded = null)
    {
        throw new NotImplementedException();
    }

    public ValueTask<float?> GetChannelVolumeAsync(IMediaChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetChannelVolumeAsync(IMediaChannel channel, float volume)
    {
        throw new NotImplementedException();
    }

    static LibVlcVideoBackendService()
    {
        Core.Initialize();
    }
}