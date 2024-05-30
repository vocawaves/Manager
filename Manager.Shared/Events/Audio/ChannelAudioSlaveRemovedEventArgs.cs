using Manager.Shared.Entities;

namespace Manager.Shared.Events.Audio;

public class ChannelAudioSlaveRemovedEventArgs : EventArgs
{
    public MediaItem RemovedItem { get; }

    public ChannelAudioSlaveRemovedEventArgs(MediaItem removedItem)
    {
        this.RemovedItem = removedItem;
    }
}