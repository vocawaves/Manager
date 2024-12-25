using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Manager2.Shared.BaseModels;

public abstract partial class AudioStream : MediaStream
{
    [ObservableProperty] private int _channels;

    [ObservableProperty] private int _sampleRate;

    [ObservableProperty] private double _bitRate;

    [ObservableProperty] private string? _codecName;

    protected AudioStream(MediaDataService mediaDataService, MediaItem mediaItem, object identifier,
        ILogger<AudioStream>? logger = default) : base(
        mediaDataService, mediaItem, identifier, logger)
    {
    }
}