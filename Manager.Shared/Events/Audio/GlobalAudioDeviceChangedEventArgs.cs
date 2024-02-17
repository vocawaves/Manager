using Manager.Shared.Entities;

namespace Manager.Shared.Events.Audio;

public class GlobalAudioDeviceChangedEventArgs : EventArgs
{
    public AudioDevice AudioDevice { get; }

    public GlobalAudioDeviceChangedEventArgs(AudioDevice audioDevice)
    {
        this.AudioDevice = audioDevice;
    }
}