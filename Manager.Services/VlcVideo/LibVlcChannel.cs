using LibVLCSharp.Shared;
using Manager.Shared;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces;

namespace Manager.Services.VlcVideo;

public class LibVlcChannel : IVideoChannel, IAudioChannel
{
    public IVideoBackendService AssociatedVideoBackend { get; }
    public IAudioBackendService AssociatedAudioBackend { get; }
    public PlayItem PlayItem { get; }
    public Media LibVlcMedia { get; }

    public LibVlcChannel(ManagerComponent component, PlayItem playItem, Media media)
    {
        if (component is not (IVideoBackendService ivs and IAudioBackendService ias))
            throw new ArgumentException("Component must be an audio or video backend service");
        
        this.AssociatedVideoBackend = ivs;
        this.AssociatedAudioBackend = ias;
        this.PlayItem = playItem;
        this.LibVlcMedia = media;
    }
    
    public ValueTask<bool> PlayAsync()
    {
        return this.AssociatedVideoBackend.PlayChannelAsync(this);
    }

    public ValueTask<bool> PauseAsync()
    {
        return this.AssociatedVideoBackend.PauseChannelAsync(this);
    }

    public ValueTask<bool> ResumeAsync()
    {
        return this.AssociatedVideoBackend.ResumeChannelAsync(this);
    }

    public ValueTask<bool> StopAsync()
    {
        return this.AssociatedVideoBackend.StopChannelAsync(this);
    }

    public ValueTask<ChannelState?> GetStateAsync()
    {
        return this.AssociatedVideoBackend.GetChannelStateAsync(this);
    }

    public ValueTask<bool> SetStateAsync(ChannelState state)
    {
        return this.AssociatedVideoBackend.SetChannelStateAsync(this, state);
    }
    
    public ValueTask<float?> GetVolumeAsync()
    {
        return this.AssociatedAudioBackend.GetChannelVolumeAsync(this);
    }

    public ValueTask<bool> SetVolumeAsync(float volume)
    {
        return this.AssociatedAudioBackend.SetChannelVolumeAsync(this, volume);
    }

    public ValueTask<AudioDevice?> GetDeviceAsync()
    {
        return this.AssociatedAudioBackend.GetChannelDeviceAsync(this);
    }

    public ValueTask<bool> SetDeviceAsync(AudioDevice device)
    {
        return this.AssociatedAudioBackend.SetChannelDeviceAsync(this, device);
    }

    public ValueTask<TimeSpan?> GetPositionAsync()
    {
        return this.AssociatedVideoBackend.GetChannelPositionAsync(this);
    }

    public ValueTask<bool> SetPositionAsync(double positionMs)
    {
        return this.AssociatedVideoBackend.SetChannelPositionAsync(this, positionMs);
    }

    public ValueTask<bool> SetPositionAsync(TimeSpan position)
    {
        return this.AssociatedVideoBackend.SetChannelPositionAsync(this, position);
    }

    public ValueTask<TimeSpan?> GetLengthAsync()
    {
        return this.AssociatedVideoBackend.GetChannelLengthAsync(this);
    }

    public ValueTask<bool> DestroyAsync()
    {
        return this.AssociatedVideoBackend.DestroyChannelAsync(this);
    }
    
    public async ValueTask DisposeAsync()
    {
        await this.DestroyAsync();
    }
}