using Manager.Shared.Entities;

namespace Manager.Shared.Events.Audio;

public class ChannelAudioSlaveAddedEventArgs : EventArgs
{
    public MediaItem AddedItem { get; }

    public ChannelAudioSlaveAddedEventArgs(MediaItem addedItem)
    {
        this.AddedItem = addedItem;
    }
}