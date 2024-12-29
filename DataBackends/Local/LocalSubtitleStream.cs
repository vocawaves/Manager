using Manager2.Shared.BaseModels;
using Manager2.Shared.Entities;
using Microsoft.Extensions.Logging;
using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Utils;

namespace Local;

public class LocalSubtitleStream : SubtitleStream
{
    public LocalSubtitleStream(LocalDataService mediaDataService, MediaItem mediaItem, object identifier,
        ILogger<SubtitleStream>? logger = null) : base(mediaDataService, mediaItem, identifier, logger)
    {
    }

    public override ValueTask<ReturnResult> ExtractStreamAsync(IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override ValueTask<ReturnResult> RemoveExtractedStreamAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override ValueTask<ReturnResult<string>> GetExtractedStreamPathAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override ValueTask<ReturnResult<Stream>> GetExtractedStreamAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}