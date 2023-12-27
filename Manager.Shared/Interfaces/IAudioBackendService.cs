using Manager.Shared.Delegates;
using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces;

public interface IAudioBackendService : IBackendService
{
    public event AudioServiceGlobalDeviceChangedEventHandler? GlobalDeviceChanged;
    public event AudioServiceChannelVolumeChangedEventHandler? ChannelVolumeChanged;
    
    public ValueTask<AudioDevice[]> GetDevicesAsync();
    public ValueTask<AudioDevice> GetCurrentlySelectedDeviceAsync();
    public ValueTask<bool> SetDeviceAsync(AudioDevice device);
    
    public ValueTask<AudioDevice?> GetChannelDeviceAsync(IMediaChannel channel);
    public ValueTask<bool> SetChannelDeviceAsync(IMediaChannel channel, AudioDevice device);
    
    public ValueTask<IMediaChannel?> CreateChannelAsync(PlayItem playItem, float volume = 1f, int frequency = 44100, AudioDevice? device = null, Action<PlayItem>? onEnded = null);
    
    public ValueTask<float?> GetChannelVolumeAsync(IMediaChannel channel);
    public ValueTask<bool> SetChannelVolumeAsync(IMediaChannel channel, float volume);
}