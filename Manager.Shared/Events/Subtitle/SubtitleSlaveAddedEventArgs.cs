using Manager.Shared.Entities;

namespace Manager.Shared.Events.Subtitle;

public class SubtitleSlaveAddedEventArgs
{
    public MediaItem SubtitleItem { get; }
    public SubtitleSlaveAddedEventArgs(MediaItem subtitleItem)
    {
        this.SubtitleItem = subtitleItem;
    }
}