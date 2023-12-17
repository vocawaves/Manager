using Manager.Shared.Delegates;
using Manager.Shared.Entities;
using Manager.Shared.Enums;

namespace Manager.Shared.Interfaces;

public interface IAudioChannel : IAsyncDisposable
{
    public IAudioBackendService AssociatedBackend { get; }
    public PlayItem PlayItem { get; }
    
    public ValueTask<float?> GetVolumeAsync();
    public ValueTask<bool> SetVolumeAsync(float volume);
    
    public ValueTask<bool> PlayAsync();
    public ValueTask<bool> PauseAsync();
    public ValueTask<bool> ResumeAsync();
    public ValueTask<bool> StopAsync();
    
    public ValueTask<ChannelState?> GetStateAsync();
    public ValueTask<bool> SetStateAsync(ChannelState state);
    
    public ValueTask<TimeSpan?> GetPositionAsync();
    public ValueTask<bool> SetPositionAsync(double positionMs);
    public ValueTask<bool> SetPositionAsync(TimeSpan position);
    
    public ValueTask<TimeSpan?> GetLengthAsync();
    
    public ValueTask<AudioDevice?> GetDeviceAsync();
    public ValueTask<bool> SetDeviceAsync(AudioDevice device);
    
    public ValueTask<bool> DestroyAsync();
}