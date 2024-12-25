using CommunityToolkit.Mvvm.ComponentModel;
using Manager2.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace Manager2.Shared.BaseModels;

public abstract partial class MediaStream : ObservableObject
{
    protected internal readonly ILogger<MediaStream>? Logger;
    
    [ObservableProperty]
    private object _identifier;
    
    [ObservableProperty]
    private TimeSpan _duration;
    
    [ObservableProperty]
    private ExtractState _extractState;
    
    [ObservableProperty]
    private double _extractProgress;
    
    [ObservableProperty]
    private string? _extractedStreamPath;
    
    public MediaDataService MediaDataService { get; set; }
    public MediaItem MediaItem { get; init; }
    
    public Dictionary<string, object> StreamMetadata { get; set; } = new();

    protected MediaStream(MediaDataService mediaDataService ,MediaItem mediaItem, object identifier, ILogger<MediaStream>? logger = null)
    {
        MediaDataService = mediaDataService;
        MediaItem = mediaItem;
        Identifier = identifier;
        Logger = logger;
    }
    
    public abstract ValueTask<bool> ExtractStreamAsync(IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
    
    public abstract ValueTask<bool> RemoveExtractedStreamAsync(CancellationToken cancellationToken = default);
    
    public abstract ValueTask<string?> GetExtractedStreamPathAsync(CancellationToken cancellationToken = default);
    
    public abstract ValueTask<Stream?> GetExtractedStreamAsync(CancellationToken cancellationToken = default);
}