using Manager.Shared.Entities;

namespace Manager.Shared.Events.General;

public class ChannelPositionTriggerInvokedEventArgs : EventArgs
{
    public PositionTrigger Trigger { get; }
    
    public ChannelPositionTriggerInvokedEventArgs(PositionTrigger trigger)
    {
        this.Trigger = trigger;
    }
}