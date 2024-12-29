using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Manager2.Shared.Entities;
using Manager2.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace Manager2.Shared.BaseModels;

public abstract partial class MediaItem : ObservableObject
{
    protected internal readonly ILogger<MediaItem>? Logger;
    
    [ObservableProperty] private string _sourcePath;

    [ObservableProperty] private string _pathTitle;
    
    [ObservableProperty] private TimeSpan _duration;
    
    [ObservableProperty] private CacheState _cacheState;
    
    [ObservableProperty] private double _cacheProgress;
    
    [ObservableProperty] private bool _isMuxed;
    
    [ObservableProperty] private bool _needsCaching;

    //[ObservableProperty]
    //private ulong _parentId;
    
    public VideoStream? DefaultVideoStream => VideoStreams.FirstOrDefault();
    public List<VideoStream> VideoStreams { get; set; } = new();
 
    public AudioStream? DefaultAudioStream => AudioStreams.FirstOrDefault();
    public List<AudioStream> AudioStreams { get; set; } = new();

    public SubtitleStream? DefaultSubtitleStream => SubtitleStreams.FirstOrDefault();
    public List<SubtitleStream> SubtitleStreams { get; set; } = new();
    
    public Dictionary<string, object> Metadata { get; set; } = new();

    protected MediaItem(string sourcePath, string pathTitle, ILogger<MediaItem>? logger = null)
    {
        SourcePath = sourcePath;
        PathTitle = pathTitle;
        Logger = logger;
    }

    public abstract ValueTask<ReturnResult> CacheAsync(IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
    
    public abstract ValueTask<ReturnResult> CacheAsync(IProgress<double>? progress = null,
        CancellationToken cancellationToken = default, params MediaStream[] streamsToCache);

    public abstract ValueTask<ReturnResult> RemoveCacheAsync(bool removeExtractedStreams = true,
        CancellationToken cancellationToken = default);

    public abstract ValueTask<ReturnResult<string>> GetCachePathAsync(CancellationToken cancellationToken = default);

    public abstract ValueTask<ReturnResult<Stream>> GetStreamAsync(CancellationToken cancellationToken = default);
}