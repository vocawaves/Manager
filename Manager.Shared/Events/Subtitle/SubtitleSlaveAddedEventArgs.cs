using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.Subtitle;

public class SubtitleSlaveAddedEventArgs
{
    public PlaybackItem PlaybackItem { get; }
    public SubtitleSlaveAddedEventArgs(PlaybackItem playbackItem)
    {
        this.PlaybackItem = playbackItem;
    }
}