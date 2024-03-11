using Manager.Shared.Entities;

namespace Manager.Shared.Events.Audio;

public class ChannelAudioSlaveAddedEventArgs : EventArgs
{
    public AudioItem AddedItem { get; }

    public ChannelAudioSlaveAddedEventArgs(AudioItem addedItem)
    {
        this.AddedItem = addedItem;
    }
}