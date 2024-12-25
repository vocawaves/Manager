using Manager2.Shared.BaseModels;
using Manager2.Shared.Enums;
using Microsoft.Extensions.Logging;
using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Raw;
using Sdcb.FFmpeg.Toolboxs.Extensions;
using Sdcb.FFmpeg.Utils;
using MediaStream = Sdcb.FFmpeg.Formats.MediaStream;

namespace Local;

public class LocalAudioStream : AudioStream
{
    public LocalAudioStream(LocalDataService mediaDataService, MediaItem mediaItem, object identifier,
        ILogger<AudioStream>? logger = default) : base(mediaDataService, mediaItem, identifier, logger)
    {
    }

    public override async ValueTask<bool> ExtractStreamAsync(IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var cachePath = MediaDataService.CachePath;
        if (string.IsNullOrWhiteSpace(cachePath))
        {
            Logger?.LogError("Cache path is not set");
            return false;
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
            var duration = stream.Duration <= 0 ? (inputFc.Duration / ffmpeg.AV_TIME_BASE) : stream.GetDurationInSeconds();
            if (duration == 0)
            {
                Logger?.LogWarning("Duration is 0");
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
        return true;
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
        ExtractState = ExtractState.NotExtracted;
        ExtractProgress = 0;
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
        
        if (!File.Exists(ExtractedStreamPath))
        {
            Logger?.LogError("Extracted stream file does not exist: {Path}", ExtractedStreamPath);
            ExtractState = ExtractState.NotExtracted;
            ExtractedStreamPath = null;
            ExtractProgress = 0;
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