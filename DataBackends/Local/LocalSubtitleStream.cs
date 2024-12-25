using Manager2.Shared.BaseModels;
using Microsoft.Extensions.Logging;
using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Utils;

namespace Local;

public class LocalSubtitleStream : SubtitleStream
{
    public LocalSubtitleStream(LocalDataService mediaDataService, MediaItem mediaItem, object identifier,
        ILogger<SubtitleStream>? logger = default) : base(mediaDataService, mediaItem, identifier, logger)
    {
    }

    public override ValueTask<bool> ExtractStreamAsync(IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override ValueTask<bool> RemoveExtractedStreamAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override ValueTask<string?> GetExtractedStreamPathAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override ValueTask<Stream?> GetExtractedStreamAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}