using Manager.Shared.Events.Subtitle;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.General;

namespace Manager.Shared.Interfaces.Subtitle;

/// <summary>
/// Defines a channel that can handle subtitles.
/// It mostly mirrors the IVideoChannel interface for now
/// Same as the ISubtitleBackendService, this is still WIP and might be subject to change.
/// </summary>
public interface ISubtitleChannel : IMediaChannel
{
    //This is all very WIP and just mirrors the IVideoChannel interface for now so no additional comments here
    #region Events

    public event AsyncEventHandler<SubtitleRenderWindowChangedEventArgs>? RenderWindowChanged;

    #endregion
    
    public ValueTask<bool> SetRenderWindowAsync(nint windowHandle);
    public ValueTask<nint> GetRenderWindowAsync();
}