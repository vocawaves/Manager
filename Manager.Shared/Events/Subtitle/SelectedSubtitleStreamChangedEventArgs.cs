using Manager.Shared.Entities;

namespace Manager.Shared.Events.Subtitle;

public class SelectedSubtitleStreamChangedEventArgs
{
    public SelectableMediaStream Stream { get; }

    public SelectedSubtitleStreamChangedEventArgs(SelectableMediaStream stream)
    {
        this.Stream = stream;
    }
}