using Manager.Shared.Entities;
using Manager.Shared.Enums;

namespace Manager.Shared.Interfaces;

public interface IMediaChannel : IAsyncDisposable
{
    public PlayItem PlayItem { get; }

    public ValueTask<bool> PlayAsync();
    public ValueTask<bool> PauseAsync();
    public ValueTask<bool> ResumeAsync();
    public ValueTask<bool> StopAsync();
    
    public ValueTask<ChannelState?> GetStateAsync();
    public ValueTask<bool> SetStateAsync(ChannelState state);
    
    public ValueTask<TimeSpan?> GetPositionAsync();
    public ValueTask<bool> SetPositionAsync(double positionMs);
    public ValueTask<bool> SetPositionAsync(TimeSpan position);
    
    public ValueTask<TimeSpan?> GetLengthAsync();
    
    public ValueTask<bool> DestroyAsync();
}