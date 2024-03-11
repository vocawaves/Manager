using Manager.Shared.Entities;

namespace Manager.Shared.Events.Subtitle;

public class SubtitleSlaveRemovedEventArgs
{
    public SubtitleItem SubtitleItem { get; }
    public SubtitleSlaveRemovedEventArgs(SubtitleItem subtitleItem)
    {
        this.SubtitleItem = subtitleItem;
    }
}