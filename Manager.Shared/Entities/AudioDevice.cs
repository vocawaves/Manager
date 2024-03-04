using Manager.Shared.Interfaces;
using Manager.Shared.Interfaces.Audio;

namespace Manager.Shared.Entities;

public class AudioDevice
{
    public IAudioBackendService AssociatedBackend { get; private set; }
    public string Name { get; private set; }
    public string Id { get; private set; }
    
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