using Manager.Shared.Enums;

namespace Manager.Shared.Events.General;

public class ChannelStateChangedEventArgs : EventArgs
{
    public ChannelState State { get; }

    public ChannelStateChangedEventArgs(ChannelState state)
    {
        this.State = state;
    }
}