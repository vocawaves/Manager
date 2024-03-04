namespace Manager.Shared.Events.General;

public class InitSuccessEventArgs : System.EventArgs
{
    public string? Message { get; }

    public InitSuccessEventArgs(string? message = null)
    {
        this.Message = message;
    }
}