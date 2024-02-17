namespace Manager.Shared.Events.Video;

public class ExternalPlayerSurfaceTargetChanged : EventArgs
{
    public IntPtr NewTarget { get; }

    public ExternalPlayerSurfaceTargetChanged(IntPtr newTarget)
    {
        this.NewTarget = newTarget;
    }
}