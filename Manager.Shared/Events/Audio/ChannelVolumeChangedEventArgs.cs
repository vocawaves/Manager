namespace Manager.Shared.Events.Audio;

public class ChannelVolumeChangedEventArgs : EventArgs
{
    public float Volume { get; }

    public ChannelVolumeChangedEventArgs(float volume)
    {
        this.Volume = volume;
    }
}