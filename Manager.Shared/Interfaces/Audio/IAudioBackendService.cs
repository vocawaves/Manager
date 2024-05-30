using Manager.Shared.Entities;
using Manager.Shared.Events.Audio;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.General;

namespace Manager.Shared.Interfaces.Audio;

/// <summary>
/// The audio backend service is responsible for managing the audio devices and volume of the system.
/// This should be per API implementation. (BASS, NAudio, etc.)
/// </summary>
public interface IAudioBackendService : IBackendService
{
    //public event AudioServiceGlobalDeviceChangedEventHandler? GlobalDeviceChanged;
    /// <summary>
    /// Fired when the global default volume changes.
    /// This usually is associated with the whole system volume.
    /// </summary>
    public event AsyncEventHandler<GlobalDefaultVolumeChangedEventArgs>? GlobalVolumeChanged; //idk if useful
    
    /// <summary>
    /// Fired when the global default device changes.
    /// Only fired if the Init device was changed.
    /// </summary>
    public event AsyncEventHandler<GlobalAudioDeviceChangedEventArgs>? GlobalDeviceChanged; 
    
    /// <summary>
    /// Get all available audio devices this service can use.
    /// </summary>
    public ValueTask<AudioDevice[]> GetDevicesAsync();
    
    /// <summary>
    /// Get the current audio device this service is using.
    /// </summary>
    public ValueTask<AudioDevice> GetCurrentDeviceAsync();
    
    /// <summary>
    /// Set the audio device this service should use.
    /// This will also fire the <see cref="GlobalDeviceChanged"/> event.
    /// </summary>
    public ValueTask<bool> SetDeviceAsync(AudioDevice device);

    /// <summary>
    /// Get the current volume of the audio device this service is using.
    /// Usually this is the system volume.
    /// </summary>
    public ValueTask<float> GetDefaultVolumeAsync();
    /// <summary>
    /// Sets the volume of the audio device this service is using.
    /// Also fires the <see cref="GlobalVolumeChanged"/> event.
    /// </summary>
    public ValueTask<bool> SetDefaultVolumeAsync(float volume);

}