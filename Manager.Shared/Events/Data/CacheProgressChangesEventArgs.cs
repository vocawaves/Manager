namespace Manager.Shared.Events.Data;

public class CacheProgressChangesEventArgs : EventArgs
{
    public double Progress { get; }
    
    public CacheProgressChangesEventArgs(double progress)
    {
        Progress = progress;
    }
}