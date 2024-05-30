using Manager.Shared.Entities;

namespace Manager.Shared.Events.Subtitle;

public class SubtitleSlaveRemovedEventArgs
{
    public MediaItem SubtitleItem { get; }
    public SubtitleSlaveRemovedEventArgs(MediaItem subtitleItem)
    {
        this.SubtitleItem = subtitleItem;
    }
}