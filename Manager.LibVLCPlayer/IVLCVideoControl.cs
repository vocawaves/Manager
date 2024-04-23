using LibVLCSharp;
using Manager.Shared.Interfaces.Video;

namespace Manager.LibVLCPlayer;

public interface IVLCVideoControl : IExternalPlayerSurface
{
    public LibVLC LibVLC { get; }
    public MediaPlayer MediaPlayer { get; }
    
    
}