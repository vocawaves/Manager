namespace Manager.Shared.Events.Subtitle;

public class SubtitleRenderWindowChangedEventArgs : EventArgs
{
    public IntPtr WindowHandle { get; }

    public SubtitleRenderWindowChangedEventArgs(nint windowHandle)
    {
        this.WindowHandle = windowHandle;
    }
}