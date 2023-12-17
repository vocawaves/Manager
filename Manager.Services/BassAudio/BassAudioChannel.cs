using Manager.Shared.Delegates;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces;

namespace Manager.Services.BassAudio;

public class BassAudioChannel : IAudioChannel
{
    public IAudioBackendService AssociatedBackend { get;}
    public PlayItem PlayItem { get; }
    public int BaseChannelHandle { get; }
    public int MixerChannelHandle { get; }
    
    internal BassAudioChannel(IAudioBackendService associatedBackend, PlayItem playItem, int baseChannelHandle, int mixerChannelHandle)
    {
        this.AssociatedBackend = associatedBackend;
        this.PlayItem = playItem;
        this.BaseChannelHandle = baseChannelHandle;
        this.MixerChannelHandle = mixerChannelHandle;
    }

    public ValueTask<float?> GetVolumeAsync()
        => this.AssociatedBackend.GetChannelVolumeAsync(this);

    public ValueTask<bool> SetVolumeAsync(float volume)
        => this.AssociatedBackend.SetChannelVolumeAsync(this, volume);

    public ValueTask<bool> PlayAsync()
        => this.AssociatedBackend.PlayChannelAsync(this);

    public ValueTask<bool> PauseAsync()
        => this.AssociatedBackend.PauseChannelAsync(this);

    public ValueTask<bool> ResumeAsync()
        => this.AssociatedBackend.ResumeChannelAsync(this);

    public ValueTask<bool> StopAsync()
        => this.AssociatedBackend.StopChannelAsync(this);

    public ValueTask<ChannelState?> GetStateAsync()
        => this.AssociatedBackend.GetChannelStateAsync(this);

    public ValueTask<bool> SetStateAsync(ChannelState state)
        => this.AssociatedBackend.SetChannelStateAsync(this, state);

    public ValueTask<TimeSpan?> GetPositionAsync()
        => this.AssociatedBackend.GetChannelPositionAsync(this);

    public ValueTask<bool> SetPositionAsync(double positionMs)
        => this.AssociatedBackend.SetChannelPositionAsync(this, positionMs);
    
    public ValueTask<bool> SetPositionAsync(TimeSpan position)
        => this.AssociatedBackend.SetChannelPositionAsync(this, position);

    public ValueTask<TimeSpan?> GetLengthAsync()
        => this.AssociatedBackend.GetChannelLengthAsync(this);

    public ValueTask<AudioDevice?> GetDeviceAsync()
        => this.AssociatedBackend.GetChannelDeviceAsync(this);

    public ValueTask<bool> SetDeviceAsync(AudioDevice device)
        => this.AssociatedBackend.SetChannelDeviceAsync(this, device);

    public ValueTask<bool> DestroyAsync()
        => this.AssociatedBackend.DestroyChannelAsync(this);

    public async ValueTask DisposeAsync()
    {
        await this.DestroyAsync();
        GC.SuppressFinalize(this);
    }
}