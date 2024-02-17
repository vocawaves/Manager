using Manager.Shared.Events.Subtitle;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.General;

namespace Manager.Shared.Interfaces.Subtitle;

public interface ISubtitleChannel : IMediaChannel
{
    #region Events

    public event AsyncEventHandler<SubtitleRenderWindowChangedEventArgs>? RenderWindowChanged;

    #endregion
    
    public ValueTask<bool> SetRenderWindowAsync(nint windowHandle);
    public ValueTask<nint> GetRenderWindowAsync();
}