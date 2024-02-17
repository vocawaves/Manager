using Manager.Shared.Interfaces;
using Manager.Shared.Interfaces.Audio;

namespace Manager.Shared.Entities;

public class AudioDevice
{
    public required IAudioBackendService AssociatedBackend { get; set; }
    public required string Name { get; set; }
    public required string Id { get; set; }
}