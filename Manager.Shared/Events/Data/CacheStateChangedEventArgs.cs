using Manager.Shared.Enums;

namespace Manager.Shared.Events.Data;

public class CacheStateChangedEventArgs : EventArgs
{
    public CacheState State { get; }

    public CacheStateChangedEventArgs(CacheState state)
    {
        State = state;
    }
}