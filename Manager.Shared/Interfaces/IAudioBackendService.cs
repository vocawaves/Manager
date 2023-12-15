using Manager.Shared.Delegates;
using Manager.Shared.Entities;
using Manager.Shared.Enums;

namespace Manager.Shared.Interfaces;

public interface IAudioBackendService
{
    public event AudioServiceGlobalDeviceChangedEventHandler? GlobalDeviceChanged;
    public event AudioServiceChannelCreatedEventHandler? ChannelCreated;
    
    public ValueTask<AudioDevice[]> GetDevicesAsync();
    public ValueTask<AudioDevice> GetCurrentlySelectedDeviceAsync();
    public ValueTask<bool> SetDeviceAsync(AudioDevice device);
    
    public ValueTask<AudioDevice> GetChannelDeviceAsync(IAudioChannel channel);
    public ValueTask<bool> SetChannelDeviceAsync(IAudioChannel channel, AudioDevice device);
    
    public ValueTask<IAudioChannel?> CreateChannelAsync(PlayItem playItem, Action<PlayItem>? onEnded = null);
    public ValueTask<IAudioChannel?> CreateChannelAsync(PlayItem playItem, float volume = 1f, Action<PlayItem>? onEnded = null);
    public ValueTask<IAudioChannel?> CreateChannelAsync(PlayItem playItem, int frequency = 44100, float volume = 1f, Action<PlayItem>? onEnded = null);
    public ValueTask<IAudioChannel?> CreateChannelAsync(PlayItem playItem, AudioDevice? device = null, int frequency = 44100, float volume = 1f, Action<PlayItem>? onEnded = null);
    public ValueTask<bool> DestroyChannelAsync(IAudioChannel channel);
    
    public ValueTask<float> GetChannelVolumeAsync(IAudioChannel channel);
    public ValueTask<bool> SetChannelVolumeAsync(IAudioChannel channel, float volume);
    
    public ValueTask<bool> PlayChannelAsync(IAudioChannel channel);
    public ValueTask<bool> PauseChannelAsync(IAudioChannel channel);
    public ValueTask<bool> ResumeChannelAsync(IAudioChannel channel);
    public ValueTask<bool> StopChannelAsync(IAudioChannel channel);
    
    public ValueTask<ChannelState> GetChannelStateAsync(IAudioChannel channel);
    public ValueTask<bool> SetChannelStateAsync(IAudioChannel channel, ChannelState state);
    
    public ValueTask<TimeSpan> GetChannelPositionAsync(IAudioChannel channel);
    public ValueTask<bool> SetChannelPositionAsync(IAudioChannel channel, double positionMs);
    
    public ValueTask<TimeSpan> GetChannelLengthAsync(IAudioChannel channel);
}