using Manager.Shared.Entities;

namespace Manager.Shared.Events.Audio;

public class ChannelSelectedAudioStreamChangedEventArgs : EventArgs
{
    public SelectableMediaStream? Stream { get; }

    public ChannelSelectedAudioStreamChangedEventArgs(SelectableMediaStream? stream = null)
    {
        this.Stream = stream;
    }
}