using Manager.Shared.Entities;

namespace Manager.Shared.Events.Audio;

public class ChannelDeviceChangedEventArgs : EventArgs
{
    public AudioDevice Device { get; }

    public ChannelDeviceChangedEventArgs(AudioDevice device)
    {
        this.Device = device;
    }
}