using CommunityToolkit.Mvvm.ComponentModel;
using Manager2.Shared.BaseModels;
using Microsoft.Extensions.Logging;

namespace Manager2.Shared;

public abstract partial class PlaybackBackend : ObservableObject, IAsyncDisposable
{
    [ObservableProperty]
    private string _name;
    
    protected ILogger<PlaybackBackend>? Logger { get; }

    public PlaybackBackend(string name, ILogger<PlaybackBackend>? logger = default)
    {
        _name = name;
        Logger = logger;
    }
    
    public abstract ValueTask<bool> IsMediaStreamSupportedAsync(MediaStream stream);
    
    public abstract ValueTask<MediaChannel?> CreateMediaChannelAsync(MediaStream stream);
    
    public abstract ValueTask<MediaChannel?> CreateMediaChannelAsync(MediaItem item);

    public virtual ValueTask DisposeAsync()
    {
        // TODO release managed resources here
        return ValueTask.CompletedTask;
    }
}