using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces;

public interface IAudioChannel : IMediaChannel
{
    public IAudioBackendService AssociatedBackend { get; }
    
    public ValueTask<float?> GetVolumeAsync();
    public ValueTask<bool> SetVolumeAsync(float volume);
    
    public ValueTask<AudioDevice?> GetDeviceAsync();
    public ValueTask<bool> SetDeviceAsync(AudioDevice device);
}