using Manager.Shared.Entities;
using Manager.Shared.Events.Audio;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.General;

namespace Manager.Shared.Interfaces.Audio;

public interface IAudioChannel : IMediaChannel
{
    #region Events

    //VolumeChanged
    public event AsyncEventHandler<ChannelVolumeChangedEventArgs>? ChannelVolumeChanged; 
    //DeviceChanged
    public event AsyncEventHandler<ChannelDeviceChangedEventArgs>? ChannelDeviceChanged;

    #endregion
    
    public ValueTask<float?> GetVolumeAsync();
    public ValueTask<bool> SetVolumeAsync(float volume);
    
    public ValueTask<AudioDevice?> GetDeviceAsync();
    public ValueTask<bool> SetDeviceAsync(AudioDevice device);
}