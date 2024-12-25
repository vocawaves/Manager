using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Manager2.Shared.BaseModels;

public abstract partial class SubtitleStream : MediaStream
{
    [ObservableProperty] private string? _language;

    [ObservableProperty] private string? _codecName;

    [ObservableProperty] private string? _title;

    [ObservableProperty] private string? _description;

    protected SubtitleStream(MediaDataService mediaDataService, MediaItem mediaItem, object identifier,
        ILogger<SubtitleStream>? logger = default) : base(
        mediaDataService, mediaItem, identifier, logger)
    {
    }
}