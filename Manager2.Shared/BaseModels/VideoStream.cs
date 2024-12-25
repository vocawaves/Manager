using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Manager2.Shared.BaseModels;

public abstract partial class VideoStream : MediaStream
{
    [ObservableProperty] private int _width;

    [ObservableProperty] private int _height;

    [ObservableProperty] private double _frameRate;

    [ObservableProperty] private double _bitRate;

    [ObservableProperty] private string? _codecName;

    protected VideoStream(MediaDataService mediaDataService, MediaItem mediaItem, object identifier,
        ILogger<VideoStream>? logger = default) : base(mediaDataService, mediaItem, identifier, logger)
    {
    }
}