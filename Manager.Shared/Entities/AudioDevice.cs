using Manager.Shared.Interfaces;
using Manager.Shared.Interfaces.Audio;

namespace Manager.Shared.Entities;

/// <summary>
/// A standardized wrapper for audio devices.
/// </summary>
public class AudioDevice
{
    /// <summary>
    /// The backend service that this device is associated with.
    /// </summary>
    public IAudioBackendService AssociatedBackend { get; }
    /// <summary>
    /// The name of the audio device. (or how it is displayed in the backend)
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// A unique identifier for the audio device.
    /// </summary>
    public string Id { get; }
    
    public AudioDevice(IAudioBackendService backend, string name, string id)
    {
        this.AssociatedBackend = backend;
        this.Name = name;
        this.Id = id;
    }
    
    public override string ToString()
    {
        return $"{Id} - {Name} -> {AssociatedBackend.Name}";
    }
}