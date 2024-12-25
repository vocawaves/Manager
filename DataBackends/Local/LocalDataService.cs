using Manager2.Shared;
using Manager2.Shared.BaseModels;
using Microsoft.Extensions.Logging;
using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Raw;
using Sdcb.FFmpeg.Toolboxs.Extensions;

namespace Local;

public partial class LocalDataService : MediaDataService
{
    private readonly ILogger<LocalDataService>? _logger;

    public LocalDataService(string cachePath, ILogger<LocalDataService>? logger = default)
    {
        _logger = logger;
        CachePath = cachePath;
        if (!string.IsNullOrWhiteSpace(cachePath) && !Directory.Exists(CachePath)) 
            Directory.CreateDirectory(CachePath);
    }
    
    public override ValueTask<MediaItem?> GetMediaItemByPathAsync(string path)
    {
        if (!File.Exists(path))
        {
            _logger?.LogError("File does not exist: {Path}", path);
            return ValueTask.FromResult<MediaItem?>(default);
        }
        
        try
        {
            using var fCtx = FormatContext.OpenInputUrl(path);
            _logger?.LogDebug("Opened file: {Path}", path);
            fCtx.LoadStreamInfo();
            _logger?.LogDebug("Loaded stream info for file: {Path}", path);
            var miLogger = LoggingHelper.CreateLogger<LocalMediaItem>();
            var mediaItem = new LocalMediaItem(this, path, Path.GetFileName(path), miLogger);
            _logger?.LogDebug("Created media item for file: {Path}", path);
            foreach (var baseMetadata in fCtx.Metadata)
            {
                _logger?.LogDebug("Adding metadata to media item: {Key}={Value}", baseMetadata.Key, baseMetadata.Value);
                mediaItem.Metadata.Add(baseMetadata.Key, baseMetadata.Value);
            }

            _logger?.LogDebug("Duration: {Duration}", fCtx.Duration);
            mediaItem.Duration = TimeSpan.FromSeconds(fCtx.Duration / 1000000);
            
            foreach (var ctxStream in fCtx.Streams)
            {
                _logger?.LogDebug("Adding stream to media item: {Index}={CodecType}", ctxStream.Index, ctxStream.Codecpar?.CodecType);
                if (ctxStream.Codecpar == null)
                {
                    _logger?.LogWarning("Stream has no codec parameters: {Index}", ctxStream.Index);
                    continue;
                }

                switch (ctxStream.Codecpar.CodecType)
                {
                    case AVMediaType.Video:
                        var videoStream = new LocalVideoStream(this, mediaItem, ctxStream.Index, LoggingHelper.CreateLogger<LocalVideoStream>());
                        foreach (var streamMetadata in ctxStream.Metadata)
                        {
                            _logger?.LogDebug("Adding metadata to video stream: {Key}={Value}", streamMetadata.Key, streamMetadata.Value);
                            videoStream.StreamMetadata.Add(streamMetadata.Key, streamMetadata.Value);
                        }
                        videoStream.Identifier = ctxStream.Index;
                        videoStream.Duration = TimeSpan.FromSeconds(ctxStream.GetDurationInSeconds());
                        videoStream.Width = ctxStream.Codecpar.Width;
                        videoStream.Height = ctxStream.Codecpar.Height;
                        videoStream.BitRate = ctxStream.Codecpar.BitRate;
                        var codec = Codec.FindDecoderById(ctxStream.Codecpar.CodecId);
                        videoStream.CodecName = codec.Name;
                        mediaItem.VideoStreams.Add(videoStream);
                        break;
                    case AVMediaType.Audio:
                        var audioStream = new LocalAudioStream(this, mediaItem, ctxStream.Index, LoggingHelper.CreateLogger<LocalAudioStream>());
                        foreach (var streamMetadata in ctxStream.Metadata)
                        {
                            _logger?.LogDebug("Adding metadata to audio stream: {Key}={Value}", streamMetadata.Key, streamMetadata.Value);
                            audioStream.StreamMetadata.Add(streamMetadata.Key, streamMetadata.Value);
                        }
                        audioStream.Identifier = ctxStream.Index;
                        audioStream.Duration = TimeSpan.FromSeconds(ctxStream.GetDurationInSeconds());
                        audioStream.Channels = ctxStream.Codecpar.ChLayout.nb_channels;
                        audioStream.SampleRate = ctxStream.Codecpar.SampleRate;
                        audioStream.BitRate = ctxStream.Codecpar.BitRate;
                        codec = Codec.FindDecoderById(ctxStream.Codecpar.CodecId);
                        audioStream.CodecName = codec.Name;
                        mediaItem.AudioStreams.Add(audioStream);
                        break;
                    case AVMediaType.Subtitle:
                        var subtitleStream = new LocalSubtitleStream(this, mediaItem, ctxStream.Index, LoggingHelper.CreateLogger<LocalSubtitleStream>());
                        foreach (var streamMetadata in ctxStream.Metadata)
                        {
                            _logger?.LogDebug("Adding metadata to subtitle stream: {Key}={Value}", streamMetadata.Key, streamMetadata.Value);
                            subtitleStream.StreamMetadata.Add(streamMetadata.Key, streamMetadata.Value);
                        }
                        subtitleStream.Identifier = ctxStream.Index;
                        subtitleStream.Duration = TimeSpan.FromSeconds(ctxStream.GetDurationInSeconds());
                        codec = Codec.FindDecoderById(ctxStream.Codecpar.CodecId);
                        subtitleStream.CodecName = codec.Name;
                        mediaItem.SubtitleStreams.Add(subtitleStream);
                        break;
                    case AVMediaType.Data:
                    case AVMediaType.Attachment:
                    case AVMediaType.Nb:
                    case AVMediaType.Unknown:
                    default:
                        _logger?.LogWarning("Not handling codec type: {CodecType}", ctxStream.Codecpar.CodecType);
                        break;
                }
            }
                
            return ValueTask.FromResult<MediaItem?>(mediaItem);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to open file: {Path}", path);
            return ValueTask.FromResult<MediaItem?>(default);
        }
    }
    
    public ValueTask<MediaItem?> GetMediaItemBySearchResultAsync(LocalSearchResult searchResult)
    {
        throw new NotImplementedException();
    }
    
    public ValueTask<IEnumerable<MediaItem>?> GetMediaItemsByPathAsync(string path)
    {
        throw new NotImplementedException();
    }
    
    public ValueTask<IEnumerable<MediaItem>?> GetMediaItemsBySearchResultsAsync(IEnumerable<LocalSearchResult> searchResults)
    {
        throw new NotImplementedException();
    }
    
    public ValueTask<IEnumerable<LocalSearchResult>?> SearchAsync(string query, string directory)
    {
        throw new NotImplementedException();
    }
}