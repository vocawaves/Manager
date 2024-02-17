using Manager.Shared.Entities;

namespace Manager.Shared.Events.Audio;

public class ChannelAudioSlaveRemovedEventArgs : EventArgs
{
    public PlaybackItem RemovedItem { get; }

    public ChannelAudioSlaveRemovedEventArgs(PlaybackItem removedItem)
    {
        this.RemovedItem = removedItem;
    }
}