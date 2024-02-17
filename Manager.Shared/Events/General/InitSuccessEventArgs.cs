namespace Manager.Shared.Events.Global;

public class InitSuccessEventArgs : System.EventArgs
{
    public string? AdditionalInfo { get; }

    public InitSuccessEventArgs(string? additionalInfo = null)
    {
        this.AdditionalInfo = additionalInfo;
    }
}