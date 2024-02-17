namespace Manager.Shared.Events.Global;

public class InitFailedEventArgs : EventArgs
{
    public Exception Exception { get; }
    public string? AdditionalInfo { get; }

    public InitFailedEventArgs(Exception exception, string? additionalInfo = null)
    {
        this.Exception = exception;
        this.AdditionalInfo = additionalInfo;
    }
}