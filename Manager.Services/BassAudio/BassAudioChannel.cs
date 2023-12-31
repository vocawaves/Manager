using Manager.Shared.Delegates;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces;

namespace Manager.Services.BassAudio;

public class BassAudioChannel : IAudioChannel
{
    public IAudioBackendService AssociatedAudioBackend { get;}
    public PlayItem PlayItem { get; }
    public int BaseChannelHandle { get; }
    public int MixerChannelHandle { get; }
    
    internal BassAudioChannel(IAudioBackendService associatedBackend, PlayItem playItem, int baseChannelHandle, int mixerChannelHandle)
    {
        this.AssociatedAudioBackend = associatedBackend;
        this.PlayItem = playItem;
        this.BaseChannelHandle = baseChannelHandle;
        this.MixerChannelHandle = mixerChannelHandle;
    }

    public ValueTask<float?> GetVolumeAsync()
        => this.AssociatedAudioBackend.GetChannelVolumeAsync(this);

    public ValueTask<bool> SetVolumeAsync(float volume)
        => this.AssociatedAudioBackend.SetChannelVolumeAsync(this, volume);

    public ValueTask<bool> PlayAsync()
        => this.AssociatedAudioBackend.PlayChannelAsync(this);

    public ValueTask<bool> PauseAsync()
        => this.AssociatedAudioBackend.PauseChannelAsync(this);

    public ValueTask<bool> ResumeAsync()
        => this.AssociatedAudioBackend.ResumeChannelAsync(this);

    public ValueTask<bool> StopAsync()
        => this.AssociatedAudioBackend.StopChannelAsync(this);

    public ValueTask<ChannelState?> GetStateAsync()
        => this.AssociatedAudioBackend.GetChannelStateAsync(this);

    public ValueTask<bool> SetStateAsync(ChannelState state)
        => this.AssociatedAudioBackend.SetChannelStateAsync(this, state);

    public ValueTask<TimeSpan?> GetPositionAsync()
        => this.AssociatedAudioBackend.GetChannelPositionAsync(this);

    public ValueTask<bool> SetPositionAsync(double positionMs)
        => this.AssociatedAudioBackend.SetChannelPositionAsync(this, positionMs);
    
    public ValueTask<bool> SetPositionAsync(TimeSpan position)
        => this.AssociatedAudioBackend.SetChannelPositionAsync(this, position);

    public ValueTask<TimeSpan?> GetLengthAsync()
        => this.AssociatedAudioBackend.GetChannelLengthAsync(this);

    public ValueTask<AudioDevice?> GetDeviceAsync()
        => this.AssociatedAudioBackend.GetChannelDeviceAsync(this);

    public ValueTask<bool> SetDeviceAsync(AudioDevice device)
        => this.AssociatedAudioBackend.SetChannelDeviceAsync(this, device);

    public ValueTask<bool> DestroyAsync()
        => this.AssociatedAudioBackend.DestroyChannelAsync(this);

    public async ValueTask DisposeAsync()
    {
        await this.DestroyAsync();
    }
}