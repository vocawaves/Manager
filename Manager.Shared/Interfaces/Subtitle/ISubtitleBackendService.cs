using Manager.Shared.Interfaces.General;

namespace Manager.Shared.Interfaces.Subtitle;

/// <summary>
/// Defines a service that can handle subtitles.
/// This is still WIP and might be subject to change.
/// </summary>
public interface ISubtitleBackendService : IBackendService
{
    //Create SubtitlePlayer(?)
    //Destroy SubtitlePlayer
    //Should have option to set WID Pointer
    //Maybe something different here as this could be achieved without a dedicated video player maybe
}