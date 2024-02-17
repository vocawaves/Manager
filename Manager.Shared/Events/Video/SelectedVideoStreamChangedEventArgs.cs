using Manager.Shared.Entities;

namespace Manager.Shared.Events.Video;

public class SelectedVideoStreamChangedEventArgs : EventArgs
{
    public SelectableMediaStream Stream { get; }

    public SelectedVideoStreamChangedEventArgs(SelectableMediaStream stream)
    {
        this.Stream = stream;
    }
}