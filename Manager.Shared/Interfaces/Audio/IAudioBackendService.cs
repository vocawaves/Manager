using Manager.Shared.Entities;
using Manager.Shared.Events.Audio;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.General;

namespace Manager.Shared.Interfaces.Audio;

public interface IAudioBackendService : IBackendService
{
    //public event AudioServiceGlobalDeviceChangedEventHandler? GlobalDeviceChanged;
    public event AsyncEventHandler<GlobalDefaultVolumeChangedEventArgs>? GlobalDefaultVolumeChanged; //idk if useful
    public event AsyncEventHandler<GlobalAudioDeviceChangedEventArgs>? GlobalDeviceCHanged; 
    //public event AudioServiceChannelVolumeChangedEventHandler? ChannelVolumeChanged;
    
    public ValueTask<AudioDevice[]?> GetDevicesAsync();
    public ValueTask<AudioDevice?> GetCurrentlySelectedDeviceAsync();
    public ValueTask<bool> SetDeviceAsync(AudioDevice device);

    public ValueTask<float> GetDefaultVolumeAsync();
    public ValueTask<bool> SetDefaultVolumeAsync(float volume);

}