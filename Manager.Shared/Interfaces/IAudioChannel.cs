using Manager.Shared.Delegates;
using Manager.Shared.Entities;
using Manager.Shared.Enums;

namespace Manager.Shared.Interfaces;

public interface IAudioChannel
{
    public event AudioServiceChannelDestroyedEventHandler? Destroyed;
    public event AudioServiceChannelVolumeChangedEventHandler? VolumeChanged;
    public event AudioServiceChannelStateChangedEventHandler? StateChanged;
    public event AudioServiceChannelPositionChangedEventHandler? PositionChanged;
    
    public IAudioBackendService AssociatedBackend { get; set; }
    public PlayItem PlayItem { get; set; }
    
    public ValueTask<float> GetVolumeAsync();
    public ValueTask<bool> SetVolumeAsync(float volume);
    
    public ValueTask<bool> PlayAsync();
    public ValueTask<bool> PauseAsync();
    public ValueTask<bool> ResumeAsync();
    public ValueTask<bool> StopAsync();
    
    public ValueTask<ChannelState> GetStateAsync();
    public ValueTask<bool> SetStateAsync(ChannelState state);
    
    public ValueTask<TimeSpan> GetPositionAsync();
    public ValueTask<bool> SetPositionAsync(double positionMs);
    
    public ValueTask<TimeSpan> GetLengthAsync();
    
    public ValueTask<AudioDevice> GetDeviceAsync();
    public ValueTask<bool> SetDeviceAsync(AudioDevice device);
    
    public ValueTask<bool> DestroyAsync();
}