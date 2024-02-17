using Manager.Shared.Entities;

namespace Manager.Shared.Events.Audio;

public class ChannelAudioSlaveAddedEventArgs : EventArgs
{
    public PlaybackItem AddedItem { get; }

    public ChannelAudioSlaveAddedEventArgs(PlaybackItem addedItem)
    {
        this.AddedItem = addedItem;
    }
}