using Manager.Shared.Interfaces.Video;

namespace Manager.Shared.Interfaces.Hacks;

public interface INeedsEmotionalSupport
{
    public bool IsPlayReady { get; }
    
    public ValueTask<bool> PrepareForPlayAsync(int syncTime = 150);
}