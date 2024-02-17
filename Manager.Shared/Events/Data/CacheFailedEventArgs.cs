namespace Manager.Shared.Events.Data;

public class CacheFailedEventArgs : EventArgs
{
    public string Message { get; }
    public Exception? Exception { get; }
    public string? AdditionalInfo { get; }

    public CacheFailedEventArgs(string message, Exception exception, string? additionalInfo = null)
    {
        this.Message = message;
        this.Exception = exception;
        this.AdditionalInfo = additionalInfo;
    }

    public CacheFailedEventArgs(string message, string? additionalInfo = null)
    {
        this.Message = message;
        this.AdditionalInfo = additionalInfo;
    }
}