using Manager2.Shared.BaseModels;
using Manager2.Shared.Enums;
using Microsoft.Extensions.Logging;
using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Raw;
using Sdcb.FFmpeg.Toolboxs.Extensions;
using Sdcb.FFmpeg.Utils;

namespace Local;

public partial class LocalVideoStream : VideoStream
{
    public LocalVideoStream(LocalDataService localDataService, LocalMediaItem mediaItem, object identifier,
        ILogger<LocalVideoStream>? logger = default) : base(
        localDataService, mediaItem, identifier, logger)
    {
    }

    public override ValueTask<bool> ExtractStreamAsync(IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var cachePath = MediaDataService.CachePath;
        if (string.IsNullOrWhiteSpace(cachePath))
        {
            Logger?.LogError("Cache path is not set");
            return ValueTask.FromResult(false);
        }

        ExtractState = ExtractState.Extracting;
        ExtractProgress = 0;
        
        cachePath = Path.Combine(cachePath, MediaItem.PathTitle + $".video{this.Identifier}");
        using var inFCtx = FormatContext.OpenInputUrl(MediaItem.SourcePath);
        inFCtx.LoadStreamInfo();
        var inStream = inFCtx.Streams.First(x => x.Index == (int)Identifier);
        var otherOutFormat = OutputFormat.Guess(fileName: MediaItem.SourcePath);
        using var outFCtx = FormatContext.AllocOutput(fileName: cachePath, format: otherOutFormat);
        var outStream = outFCtx.NewStream();
        outStream.Codecpar!.CopyFrom(inStream.Codecpar!);
        outStream.TimeBase = inStream.TimeBase;

        using var ioCtx = IOContext.OpenWrite(cachePath);
        outFCtx.Pb = ioCtx;

        outFCtx.WriteHeader();
        
        var fullDurationSeconds = inStream.GetDurationInSeconds() <= 0 ? (double)inFCtx.Duration / ffmpeg.AV_TIME_BASE : inStream.GetDurationInSeconds();

        var packet = new Packet();
        var pktCount = 0;
        while (inFCtx.ReadFrame(packet) != CodecResult.EOF)
        {
            if (packet.StreamIndex == inStream.Index)
            {
                packet.RescaleTimestamp(inStream.TimeBase, outStream.TimeBase);

                if (packet.Pts < 0 || packet.Dts < 0)
                {
                    Logger?.LogWarning("Invalid packet timestamps: {Pts} {Dts}", packet.Pts, packet.Dts);
                    continue;
                }
                
                pktCount++;
                //idk how else but its ok enough
                progress?.Report(Math.Clamp((pktCount * packet.Duration * inFCtx.Streams.Count / fullDurationSeconds) / 100, 0, 1));
                ExtractProgress = Math.Clamp((pktCount * packet.Duration * inFCtx.Streams.Count / fullDurationSeconds) / 100, 0, 1);
                
                packet.StreamIndex = 0;
                try
                {
                    outFCtx.InterleavedWritePacket(packet);
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, "Failed to write packet");
                }
            }
            packet.Unref();
        }

        packet.Free();
        outFCtx.WriteTrailer();

        ExtractedStreamPath = cachePath;
        ExtractProgress = 1;
        ExtractState = ExtractState.Extracted;
        progress?.Report(1);
        return ValueTask.FromResult(false);
    }

    public override ValueTask<bool> RemoveExtractedStreamAsync(CancellationToken cancellationToken = default)
    {
        if (ExtractState != ExtractState.Extracted)
        {
            Logger?.LogDebug("Stream is not extracted");
            return ValueTask.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(ExtractedStreamPath))
        {
            Logger?.LogError("Extracted stream path is not set");
            return ValueTask.FromResult(false);
        }

        if (!File.Exists(ExtractedStreamPath))
        {
            Logger?.LogError("Extracted stream file does not exist: {Path}", ExtractedStreamPath);
            return ValueTask.FromResult(false);
        }

        Logger?.LogDebug("Removing extracted stream: {Path}", ExtractedStreamPath);
        File.Delete(ExtractedStreamPath);
        Logger?.LogInformation("Removed extracted stream: {Path}", ExtractedStreamPath);
        ExtractedStreamPath = null;
        ExtractProgress = 0;
        ExtractState = ExtractState.NotExtracted;
        return ValueTask.FromResult(true);
    }

    public override ValueTask<string?> GetExtractedStreamPathAsync(CancellationToken cancellationToken = default)
    {
        if (ExtractState != ExtractState.Extracted)
        {
            Logger?.LogDebug("Stream is not extracted");
            return ValueTask.FromResult<string?>(null);
        }

        if (ExtractState == ExtractState.Extracting)
        {
            Logger?.LogDebug("Stream is extracting");
            return ValueTask.FromResult<string?>(null);
        }

        if (string.IsNullOrWhiteSpace(ExtractedStreamPath))
        {
            Logger?.LogError("Extracted stream path is not set");
            return ValueTask.FromResult<string?>(null);
        }
        
        return ValueTask.FromResult<string?>(ExtractedStreamPath);
    }

    public override ValueTask<Stream?> GetExtractedStreamAsync(CancellationToken cancellationToken = default)
    {
        if (ExtractState != ExtractState.Extracted)
        {
            Logger?.LogDebug("Stream is not extracted");
            return ValueTask.FromResult<Stream?>(null);
        }

        if (ExtractState == ExtractState.Extracting)
        {
            Logger?.LogDebug("Stream is extracting");
            return ValueTask.FromResult<Stream?>(null);
        }

        if (string.IsNullOrWhiteSpace(ExtractedStreamPath))
        {
            Logger?.LogError("Extracted stream path is not set");
            return ValueTask.FromResult<Stream?>(null);
        }

        if (!File.Exists(ExtractedStreamPath))
        {
            Logger?.LogError("Extracted stream file does not exist: {Path}", ExtractedStreamPath);
            ExtractState = ExtractState.NotExtracted;
            ExtractedStreamPath = null;
            ExtractProgress = 0;
            return ValueTask.FromResult<Stream?>(null);
        }

        return ValueTask.FromResult<Stream?>(File.OpenRead(ExtractedStreamPath));
    }
}