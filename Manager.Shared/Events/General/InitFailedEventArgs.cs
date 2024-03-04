namespace Manager.Shared.Events.General;

public class InitFailedEventArgs : EventArgs
{
    public Exception? Exception { get; }
    public string? Message { get; }

    public InitFailedEventArgs(Exception exception, string? message = null)
    {
        this.Exception = exception;
        this.Message = message;
    }
    
    public InitFailedEventArgs(string? message = null)
    {
        this.Message = message;
    }
}