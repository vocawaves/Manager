using Manager.Shared.Delegates;
using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces;

public interface IAudioBackendService
{
    public event AudioServiceGlobalDeviceChangedEventHandler? GlobalDeviceChanged;
    public event AudioServiceChannelCreatedEventHandler? ChannelCreated;
    
    public ValueTask<AudioDevice[]> GetDevicesAsync();
    public ValueTask<AudioDevice> GetCurrentlySelectedDeviceAsync();
    public ValueTask<bool> SetDeviceAsync(AudioDevice device);
    
    public ValueTask<AudioChannel?> CreateChannelAsync(PlayItem playItem, Action<PlayItem>? onEnded = null);
    public ValueTask<AudioChannel?> CreateChannelAsync(PlayItem playItem, float volume = 1f, Action<PlayItem>? onEnded = null);
    public ValueTask<AudioChannel?> CreateChannelAsync(PlayItem playItem, int frequency = 44100, float volume = 1f, Action<PlayItem>? onEnded = null);
    public ValueTask<AudioChannel?> CreateChannelAsync(PlayItem playItem, AudioDevice? device = null, int frequency = 44100, float volume = 1f, Action<PlayItem>? onEnded = null);
    public ValueTask<bool> DestroyChannelAsync(AudioChannel channel);
    
    public ValueTask<float> GetChannelVolumeAsync(AudioChannel channel);
    public ValueTask<bool> SetChannelVolumeAsync(AudioChannel channel, float volume);
    
    public ValueTask<bool> PlayChannelAsync(AudioChannel channel);
    public ValueTask<bool> PauseChannelAsync(AudioChannel channel);
    public ValueTask<bool> ResumeChannelAsync(AudioChannel channel);
    public ValueTask<bool> StopChannelAsync(AudioChannel channel);
    
    public ValueTask<ChannelState> GetChannelStateAsync(AudioChannel channel);
    public ValueTask<bool> SetChannelStateAsync(AudioChannel channel, ChannelState state);
    
    public ValueTask<double> GetChannelPositionAsync(AudioChannel channel);
    public ValueTask<bool> SetChannelPositionAsync(AudioChannel channel, double positionMs);
}

public enum ChannelState
{
    Stopped,
    Playing,
    Paused
}