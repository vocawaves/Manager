using Manager.Shared.Events.Video;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.Video;

public interface IExternalPlayerSurface
{
    public string ScreenName { get; }

    public IntPtr CurrentTarget { get; }
    
    public event AsyncEventHandler<ExternalPlayerSurfaceTargetChanged>? TargetChanged;
    
    public ValueTask<bool> SetTargetAsync(IntPtr target);
}