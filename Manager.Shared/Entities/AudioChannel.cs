using Manager.Shared.Delegates;
using Manager.Shared.Interfaces;

namespace Manager.Shared.Entities;

public class AudioChannel
{
    public event AudioServiceChannelDestroyedEventHandler? Destroyed;
    public event AudioServiceChannelVolumeChangedEventHandler? VolumeChanged;
    public event AudioServiceChannelStateChangedEventHandler? StateChanged;
    public event AudioServiceChannelPositionChangedEventHandler? PositionChanged;
    
    public required IAudioBackendService AssociatedBackend { get; set; }
    public required long Identifier { get; set; }
    public required PlayItem PlayItem { get; set; }
}