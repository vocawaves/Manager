using Manager.Shared.Entities;

namespace Manager.Shared.Events.Subtitle;

public class SubtitleSlaveAddedEventArgs
{
    public SubtitleItem SubtitleItem { get; }
    public SubtitleSlaveAddedEventArgs(SubtitleItem subtitleItem)
    {
        this.SubtitleItem = subtitleItem;
    }
}