using Manager.Shared.Interfaces;

namespace Manager.Shared.Entities;

public class AudioDevice
{
    public required IAudioBackendService AudioBackend { get; set; }
    public required string Name { get; set; }
    public required int Id { get; set; }
}