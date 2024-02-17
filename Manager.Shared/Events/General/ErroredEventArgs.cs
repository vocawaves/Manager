namespace Manager.Shared.Events.General;

public class ErroredEventArgs : EventArgs
{
    public string Method { get; }
    public string? AdditionalInfo { get; }
    public Exception? Exception { get; }

    public ErroredEventArgs(string method, string? additionalInfo = null)
    {
        this.Method = method;
        this.AdditionalInfo = additionalInfo;
    }

    public ErroredEventArgs(Exception exception, string method, string? additionalInfo = null)
    {
        this.Exception = exception;
        this.Method = method;
        this.AdditionalInfo = additionalInfo;
    }
}