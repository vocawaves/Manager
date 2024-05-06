using LibVLCSharp;
using LibVLCSharp.Shared;
using Manager.Shared.Interfaces.Hacks;
using Manager.Shared.Interfaces.Video;

namespace Manager.MediaBackends.LibVLCPlayer;

public interface IVLCVideoControl : IExternalPlayerSurface, INeedsEmotionalSupport
{
    public MediaPlayer MediaPlayer { get; }
}