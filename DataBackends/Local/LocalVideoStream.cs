using Manager2.Shared.BaseModels;
using Manager2.Shared.Entities;
using Manager2.Shared.Enums;
using Microsoft.Extensions.Logging;
using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Raw;
using Sdcb.FFmpeg.Toolboxs.Extensions;
using Sdcb.FFmpeg.Utils;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Local;

public partial class LocalVideoStream : VideoStream
{
    public LocalVideoStream(LocalDataService localDataService, LocalMediaItem mediaItem, object identifier,
        ILogger<LocalVideoStream>? logger = default) : base(
        localDataService, mediaItem, identifier, logger)
    {
    }

    public override ValueTask<ReturnResult> ExtractStreamAsync(IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ReturnResult();
        
        var cachePath = MediaDataService.CachePath;
        if (string.IsNullOrWhiteSpace(cachePath))
        {
            Logger?.LogError("Cache path is not set");
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Cache path is not set"));
            return ValueTask.FromResult(result);
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
                    result.Messages.Add(new ReturnMessage(LogLevel.Warning, "Invalid packet timestamps: {Pts} {Dts}", packet.Pts, packet.Dts));
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
        result.Success = true;
        return ValueTask.FromResult(result);
    }

    public override ValueTask<ReturnResult> RemoveExtractedStreamAsync(CancellationToken cancellationToken = default)
    {
        var result = new ReturnResult();
        
        if (ExtractState != ExtractState.Extracted)
        {
            Logger?.LogError("Stream is not extracted");
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Stream is not extracted"));
            return ValueTask.FromResult(result);
        }

        if (string.IsNullOrWhiteSpace(ExtractedStreamPath))
        {
            Logger?.LogError("Extracted stream path is not set");
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Extracted stream path is not set"));
            return ValueTask.FromResult(result);
        }

        if (!File.Exists(ExtractedStreamPath))
        {
            Logger?.LogError("Extracted stream file does not exist: {Path}", ExtractedStreamPath);
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Extracted stream file does not exist: {Path}", ExtractedStreamPath));
            return ValueTask.FromResult(result);
        }

        Logger?.LogDebug("Removing extracted stream: {Path}", ExtractedStreamPath);
        try
        {
            File.Delete(ExtractedStreamPath);
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to remove extracted stream: {Path}", ExtractedStreamPath);
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Failed to remove extracted stream: {Path}", ExtractedStreamPath));
            return ValueTask.FromResult(result);
        }
        Logger?.LogInformation("Removed extracted stream: {Path}", ExtractedStreamPath);
        ExtractedStreamPath = null;
        ExtractProgress = 0;
        ExtractState = ExtractState.NotExtracted;
        result.Success = true;
        return ValueTask.FromResult(result);
    }

    public override ValueTask<ReturnResult<string>> GetExtractedStreamPathAsync(CancellationToken cancellationToken = default)
    {
        var result = new ReturnResult<string>();
        
        if (ExtractState != ExtractState.Extracted)
        {
            Logger?.LogError("Stream is not extracted");
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Stream is not extracted"));
            return ValueTask.FromResult(result);
        }

        if (ExtractState == ExtractState.Extracting)
        {
            Logger?.LogError("Stream is extracting");
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Stream is extracting"));
            return ValueTask.FromResult(result);
        }

        if (string.IsNullOrWhiteSpace(ExtractedStreamPath) || !File.Exists(ExtractedStreamPath))
        {
            Logger?.LogError("Extracted stream path is not set or file does not exist: {Path}", ExtractedStreamPath);
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Extracted stream path is not set or file does not exist: {Path}", ExtractedStreamPath));
            return ValueTask.FromResult(result);
        }
        
        result.Value = ExtractedStreamPath;
        result.Success = true;
        return ValueTask.FromResult(result);
    }

    public override ValueTask<ReturnResult<Stream>> GetExtractedStreamAsync(CancellationToken cancellationToken = default)
    {
        var result = new ReturnResult<Stream>();
        
        if (ExtractState != ExtractState.Extracted)
        {
            Logger?.LogError("Stream is not extracted");
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Stream is not extracted"));
            return ValueTask.FromResult(result);
        }

        if (ExtractState == ExtractState.Extracting)
        {
            Logger?.LogError("Stream is extracting");
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Stream is extracting"));
            return ValueTask.FromResult(result);
        }

        if (string.IsNullOrWhiteSpace(ExtractedStreamPath) || !File.Exists(ExtractedStreamPath))
        {
            Logger?.LogError("Extracted stream path is not set or file does not exist: {Path}", ExtractedStreamPath);
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Extracted stream path is not set or file does not exist: {Path}", ExtractedStreamPath));
            return ValueTask.FromResult(result);
        }
        
        try
        {
            var stream = File.OpenRead(ExtractedStreamPath);
            result.Value = stream;
            result.Success = true;
            return ValueTask.FromResult(result);
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to open extracted stream: {Path}", ExtractedStreamPath);
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Failed to open extracted stream: {Path}", ExtractedStreamPath));
            return ValueTask.FromResult(result);
        }
    }
}