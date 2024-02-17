using Manager.Shared.Entities;

namespace Manager.Shared.Events.Subtitle;

public class SubtitleSlaveRemovedEventArgs
{
    public PlaybackItem PlaybackItem { get; }
    public SubtitleSlaveRemovedEventArgs(PlaybackItem playbackItem)
    {
        this.PlaybackItem = playbackItem;
    }
}