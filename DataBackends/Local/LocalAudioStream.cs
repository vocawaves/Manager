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
using MediaStream = Sdcb.FFmpeg.Formats.MediaStream;

namespace Local;

public class LocalAudioStream : AudioStream
{
    public LocalAudioStream(LocalDataService mediaDataService, MediaItem mediaItem, object identifier,
        ILogger<AudioStream>? logger = null) : base(mediaDataService, mediaItem, identifier, logger)
    {
    }

    public override async ValueTask<ReturnResult> ExtractStreamAsync(IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ReturnResult();
        var cachePath = MediaDataService.CachePath;
        if (string.IsNullOrWhiteSpace(cachePath))
        {
            Logger?.LogError("Cache path is not set");
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Cache path is not set"));
            return result;
        }

        cachePath = Path.Combine(cachePath, MediaItem.PathTitle + $".audio{this.Identifier}");
        //if (File.Exists(cachePath))
        //{
        //    Logger?.LogDebug("Cache file already exists: {Path}", cachePath);
        //    progress?.Report(1);
        //    if (ExtractState != ExtractState.Extracted)
        //        ExtractState = ExtractState.Extracted;
        //    return true;
        //}

        ExtractState = ExtractState.Extracting;
        Logger?.LogInformation("Extracting stream: {Index}", Identifier);
        using var inputFc = FormatContext.OpenInputUrl(MediaItem.SourcePath);
        inputFc.LoadStreamInfo();
        var stream = inputFc.Streams.First(x => x.Index == (int)Identifier);
        var codec = Codec.FindDecoderById(stream.Codecpar!.CodecId);
        Logger?.LogDebug("Found codec: {Codec}", codec.Name);
        using var decoder = new CodecContext(codec);
        decoder.FillParameters(stream.Codecpar);
        decoder.Open();
        Logger?.LogDebug("Opened decoder");
        decoder.ChLayout = stream.Codecpar.ChLayout;

        using var outputFc = FormatContext.AllocOutput(formatName: "wav", fileName: cachePath);
        var outCodec = Codec.FindEncoderByName("pcm_s16le");
        outputFc.AudioCodec = outCodec;
        var outStream = outputFc.NewStream(outCodec);
        using var encoder = new CodecContext(outCodec);
        encoder.ChLayout = decoder.ChLayout;
        encoder.SampleFormat = outputFc.AudioCodec!.Value.NegociateSampleFormat(AVSampleFormat.S16);
        encoder.SampleRate = outputFc.AudioCodec!.Value.NegociateSampleRates(decoder.SampleRate);
        encoder.TimeBase = outStream.TimeBase;
        encoder.Open(outputFc.AudioCodec);
        outStream.Codecpar!.CopyFrom(encoder);

        using var ioCtx = IOContext.OpenWrite(cachePath);
        outputFc.Pb = ioCtx;
        outputFc.WriteHeader();

        MediaThreadQueue<Frame> decoderQueue = inputFc
            .ReadPackets(stream.Index)
            .DecodeAllPackets(inputFc, audioCodec: decoder)
            .ToThreadQueue(boundedCapacity: 256);

        MediaThreadQueue<Packet> encoderQueue = decoderQueue.GetConsumingEnumerable()
            .ConvertFrames(encoder)
            //.AudioFifo(encoder)
            .EncodeFrames(encoder)
            .ToThreadQueue(boundedCapacity: 256);

        //progressTask
        Dictionary<int, PtsDts> ptsDts = new();
        var progressCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            var duration = stream.Duration <= 0
                ? (inputFc.Duration / ffmpeg.AV_TIME_BASE)
                : stream.GetDurationInSeconds();
            if (duration == 0)
            {
                Logger?.LogWarning("Duration is 0, cant progress report");
                result.Messages.Add(new ReturnMessage(LogLevel.Warning, "Duration is 0, cant progress report"));
                return;
            }

            try
            {
                while (!progressCts.Token.IsCancellationRequested)
                {
                    var progressInPercent =
                        (outStream.TimeBase * ptsDts.GetValueOrDefault(outStream.Index, PtsDts.Default).Dts)
                        .ToDouble() / duration;
                    var clampProgress = Math.Clamp(progressInPercent, 0, 1);
                    progress?.Report(clampProgress);
                    ExtractProgress = clampProgress;
                    Logger?.LogDebug(GetStatusText());
                    await Task.Delay(1, progressCts.Token);
                }
            }
            finally
            {
                Logger?.LogDebug(GetStatusText());
            }

            string GetStatusText() =>
                $"{(outStream.TimeBase * ptsDts.GetValueOrDefault(outStream.Index, PtsDts.Default).Dts).ToDouble():F2} of {duration:F2}";
        }, progressCts.Token);

        encoderQueue.GetConsumingEnumerable()
            .RecordPtsDts(ptsDts)
            .WriteAll(outputFc);

        await progressCts.CancelAsync();
        outputFc.WriteTrailer();
        if (ExtractState != ExtractState.Extracted)
            ExtractState = ExtractState.Extracted;
        ExtractProgress = 1;
        progress?.Report(1);
        Logger?.LogInformation("Extracted stream: {Index}", Identifier);
        ExtractedStreamPath = cachePath;
        result.Success = true;
        return result;
    }

    public override ValueTask<ReturnResult> RemoveExtractedStreamAsync(CancellationToken cancellationToken = default)
    {
        var result = new ReturnResult();
        if (ExtractState != ExtractState.Extracted)
        {
            Logger?.LogInformation("Stream is not extracted, cant remove");
            result.Messages.Add(new ReturnMessage(LogLevel.Information, "Stream is not extracted, cant remove"));
            return ValueTask.FromResult(result);
        }

        if (string.IsNullOrWhiteSpace(ExtractedStreamPath))
        {
            Logger?.LogError("Extracted stream path is not set, but state is extracted");
            result.Messages.Add(new ReturnMessage(LogLevel.Warning,
                "Extracted stream path is not set, but state is extracted"));
        }
        else if (!File.Exists(ExtractedStreamPath))
        {
            Logger?.LogWarning("Extracted stream file does not exist: {Path}", ExtractedStreamPath);
            result.Messages.Add(new ReturnMessage(LogLevel.Warning,
                $"Extracted stream file does not exist: {ExtractedStreamPath}"));
        }
        else
        {
            try
            {
                File.Delete(ExtractedStreamPath);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Failed to remove extracted stream: {Path}", ExtractedStreamPath);
                result.Messages.Add(new ReturnMessage(LogLevel.Error,
                    $"Failed to remove extracted stream: {ExtractedStreamPath}"));
                return ValueTask.FromResult(result);
            }
        }

        Logger?.LogDebug("Removing extracted stream: {Path}", ExtractedStreamPath);
        Logger?.LogInformation("Removed extracted stream: {Path}", ExtractedStreamPath);
        ExtractedStreamPath = null;
        ExtractState = ExtractState.NotExtracted;
        ExtractProgress = 0;
        result.Success = true;
        return ValueTask.FromResult(result);
    }

    public override ValueTask<ReturnResult<string>> GetExtractedStreamPathAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new ReturnResult<string>();
        switch (ExtractState)
        {
            case ExtractState.NotExtracted:
                Logger?.LogError("Stream is not extracted");
                result.Messages.Add(new ReturnMessage(LogLevel.Error, "Stream is not extracted"));
                return ValueTask.FromResult(result);
            case ExtractState.Extracting:
                Logger?.LogError("Stream is extracting");
                result.Messages.Add(new ReturnMessage(LogLevel.Error, "Stream is extracting"));
                return ValueTask.FromResult(result);
        }

        if (string.IsNullOrWhiteSpace(ExtractedStreamPath) || !File.Exists(ExtractedStreamPath))
        {
            Logger?.LogError("Extracted stream path is not set");
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Extracted stream path is not set"));
            ExtractState = ExtractState.NotExtracted;
            ExtractedStreamPath = null;
            ExtractProgress = 0;
            return ValueTask.FromResult(result);
        }

        result.Value = ExtractedStreamPath;
        result.Success = true;
        return ValueTask.FromResult(result);
    }

    public override ValueTask<ReturnResult<Stream>> GetExtractedStreamAsync(CancellationToken cancellationToken = default)
    {
        var result = new ReturnResult<Stream>();
        switch (ExtractState)
        {
            case ExtractState.NotExtracted:
                Logger?.LogError("Stream is not extracted");
                result.Messages.Add(new ReturnMessage(LogLevel.Error, "Stream is not extracted"));
                return ValueTask.FromResult(result);
            case ExtractState.Extracting:
                Logger?.LogError("Stream is extracting");
                result.Messages.Add(new ReturnMessage(LogLevel.Error, "Stream is extracting"));
                return ValueTask.FromResult(result);
        }

        if (string.IsNullOrWhiteSpace(ExtractedStreamPath) || !File.Exists(ExtractedStreamPath))
        {
            Logger?.LogError("Extracted stream path is not set or file does not exist");
            result.Messages.Add(new ReturnMessage(LogLevel.Error, $"Extracted stream path is not set or file does not exist"));
            ExtractState = ExtractState.NotExtracted;
            ExtractedStreamPath = null;
            ExtractProgress = 0;
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
            result.Messages.Add(new ReturnMessage(LogLevel.Error, $"Failed to open extracted stream: {ExtractedStreamPath}"));
            return ValueTask.FromResult(result);
        }
    }
}