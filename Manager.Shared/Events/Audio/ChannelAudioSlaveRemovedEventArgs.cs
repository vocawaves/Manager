using Manager.Shared.Entities;

namespace Manager.Shared.Events.Audio;

public class ChannelAudioSlaveRemovedEventArgs : EventArgs
{
    public AudioItem RemovedItem { get; }

    public ChannelAudioSlaveRemovedEventArgs(AudioItem removedItem)
    {
        this.RemovedItem = removedItem;
    }
}