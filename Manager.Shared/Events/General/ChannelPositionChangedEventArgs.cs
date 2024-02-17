namespace Manager.Shared.Events.General;

public class ChannelPositionChangedEventArgs : EventArgs
{
    public TimeSpan Position { get; }

    public ChannelPositionChangedEventArgs(TimeSpan position)
    {
        this.Position = position;
    }
}