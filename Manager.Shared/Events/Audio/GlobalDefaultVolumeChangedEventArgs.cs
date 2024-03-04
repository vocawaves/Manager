namespace Manager.Shared.Events.Audio;

public class GlobalDefaultVolumeChangedEventArgs : EventArgs
{
    public float Volume { get; }

    public GlobalDefaultVolumeChangedEventArgs(float volume)
    {
        this.Volume = volume;
    }
}