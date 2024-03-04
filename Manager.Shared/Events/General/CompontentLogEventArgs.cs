namespace Manager.Shared.Events.General;

public class CompontentLogEventArgs : EventArgs
{
    public string Message { get; }
    public CompontentLogEventArgs(string message)
    {
        this.Message = message;
    }
}