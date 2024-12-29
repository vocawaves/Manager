using Manager2.Shared.BaseModels;
using Manager2.Shared.Entities;
using Manager2.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace Local;

public partial class LocalMediaItem : MediaItem
{
    private readonly LocalDataService _dataService;

    public LocalMediaItem(LocalDataService dataService, string sourcePath, string pathTitle,
        ILogger<LocalMediaItem>? logger = default) : base(sourcePath, pathTitle, logger)
    {
        _dataService = dataService;
        SourcePath = sourcePath;
        PathTitle = pathTitle;
    }

    public override async ValueTask<ReturnResult> CacheAsync(IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ReturnResult();
        
        var cachePath = _dataService.CachePath;
        if (string.IsNullOrWhiteSpace(cachePath))
        {
            Logger?.LogError("Cache path is not set");
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Cache path is not set"));
            return result;
        }

        cachePath = Path.Combine(cachePath, PathTitle);
        if (File.Exists(cachePath))
        {
            Logger?.LogInformation("Cache file already exists: {Path}", cachePath);
            result.Messages.Add(new ReturnMessage(LogLevel.Information, "Cache file already exists"));
            if (progress != null)
                progress.Report(1);
            if (CacheState != CacheState.Cached)
            {
                CacheState = CacheState.Cached;
                CacheProgress = 1;
            }

            result.Success = true;
            return result;
        }

        Logger?.LogDebug("Caching file: {SourcePath} to {CachePath}", SourcePath, cachePath);
        try
        {
            var inFs = new FileStream(SourcePath, FileMode.Open, FileAccess.Read);
            var outFs = new FileStream(cachePath, FileMode.Create, FileAccess.Write);
            var buffer = new byte[81920];
            var totalBytes = inFs.Length;
            var totalRead = 0L;
            int bytesRead;
            Logger?.LogDebug("Total bytes: {TotalBytes}", totalBytes);
            while ((bytesRead = await inFs.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await outFs.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalRead += bytesRead;
                Logger?.LogDebug("Read {BytesRead} of {TotalBytes}", totalRead, totalBytes);
                progress?.Report((double)totalRead / totalBytes);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to cache file: {SourcePath} to {CachePath}", SourcePath, cachePath);
            result.Messages.Add(new ReturnMessage(LogLevel.Error, $"Failed to cache file: {SourcePath} to {cachePath}"));
            return result;
        }
        
        Logger?.LogInformation("Cached file: {SourcePath} to {CachePath}", SourcePath, cachePath);
        progress?.Report(1);
        CacheState = CacheState.Cached;
        CacheProgress = 1;
        result.Success = true;
        return result;
    }

    public override async ValueTask<ReturnResult> CacheAsync(IProgress<double>? progress = null,
        CancellationToken cancellationToken = default,
        params MediaStream[] streamsToCache)
    {
        var result = new ReturnResult();
        
        if (streamsToCache.Length == 0)
        {
            Logger?.LogError("No streams to cache");
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "No streams to cache"));
            return result;
        }
        var failedStreams = new List<MediaStream>();
        foreach (var mediaStream in streamsToCache)
        {
            Logger?.LogDebug("Extracting stream: {Stream} from {SourcePath}", mediaStream.Identifier, SourcePath);
            var couldExtract = await mediaStream.ExtractStreamAsync(progress, cancellationToken);
            result.Messages.AddRange(couldExtract.Messages);
            if (!couldExtract)
            {
                Logger?.LogError("Failed to extract stream: {Stream} from {SourcePath}", mediaStream.Identifier, SourcePath);
                result.Messages.Add(new ReturnMessage(LogLevel.Error, $"Failed to extract stream: {mediaStream.Identifier} from {SourcePath}"));
                failedStreams.Add(mediaStream);
                continue;
            }
            Logger?.LogInformation("Extracted stream: {Stream} from {SourcePath}", mediaStream.Identifier, SourcePath);
        }

        if (failedStreams.Count == 0)
        {
            Logger?.LogInformation("Extracted {Count} streams from {SourcePath}", streamsToCache.Length, SourcePath);
            result.Success = true;
            return result;
        }

        Logger?.LogError("Failed to extract {Count} streams from {SourcePath}", failedStreams.Count, SourcePath);
        result.Messages.Add(new ReturnMessage(LogLevel.Error, $"Failed to extract {failedStreams.Count} streams from {SourcePath}"));
        return result;
    }

    public override async ValueTask<ReturnResult> RemoveCacheAsync(bool removeExtractedStreams = true,
        CancellationToken cancellationToken = default)
    {
        var result = new ReturnResult();
        if (CacheState != CacheState.Cached && CacheState != CacheState.Caching && removeExtractedStreams == false)
        {
            Logger?.LogInformation("Media item is not cached: {SourcePath}", SourcePath);
            result.Messages.Add(new ReturnMessage(LogLevel.Information, "Media item is not cached"));
            return result;
        }
        
        if (removeExtractedStreams)
        {
            foreach (var videoStream in VideoStreams)
            {
                if (videoStream.ExtractState != ExtractState.Extracted) 
                    continue;
                Logger?.LogDebug("Removing extracted video stream: {Stream} from {SourcePath}", videoStream.Identifier, SourcePath);
                var removed = await videoStream.RemoveExtractedStreamAsync(cancellationToken);
                result.Messages.AddRange(removed.Messages);
                if (removed)
                    Logger?.LogDebug("Removed extracted video stream: {Stream} from {SourcePath}", videoStream.Identifier, SourcePath);
                else
                {
                    Logger?.LogError("Failed to remove extracted video stream: {Stream} from {SourcePath}", videoStream.Identifier, SourcePath);
                    result.Messages.Add(new ReturnMessage(LogLevel.Error, $"Failed to remove extracted video stream: {videoStream.Identifier} from {SourcePath}"));
                }
            }
            foreach (var audioStream in AudioStreams)
            {
                if (audioStream.ExtractState != ExtractState.Extracted) 
                    continue;
                Logger?.LogDebug("Removing extracted audio stream: {Stream} from {SourcePath}", audioStream.Identifier, SourcePath);
                var removed = await audioStream.RemoveExtractedStreamAsync(cancellationToken);
                result.Messages.AddRange(removed.Messages);
                if (removed)
                    Logger?.LogInformation("Removed extracted audio stream: {Stream} from {SourcePath}", audioStream.Identifier, SourcePath);
                else
                {
                    Logger?.LogError("Failed to remove extracted audio stream: {Stream} from {SourcePath}", audioStream.Identifier, SourcePath);
                    result.Messages.Add(new ReturnMessage(LogLevel.Error, $"Failed to remove extracted audio stream: {audioStream.Identifier} from {SourcePath}"));
                }
            }
            foreach (var subtitleStream in SubtitleStreams)
            {
                if (subtitleStream.ExtractState != ExtractState.Extracted) 
                    continue;
                Logger?.LogDebug("Removing extracted subtitle stream: {Stream} from {SourcePath}", subtitleStream.Identifier, SourcePath);
                var removed = await subtitleStream.RemoveExtractedStreamAsync(cancellationToken);
                result.Messages.AddRange(removed.Messages);
                if (removed)
                    Logger?.LogInformation("Removed extracted subtitle stream: {Stream} from {SourcePath}", subtitleStream.Identifier, SourcePath);
                else
                {
                    Logger?.LogError("Failed to remove extracted subtitle stream: {Stream} from {SourcePath}", subtitleStream.Identifier, SourcePath);
                    result.Messages.Add(new ReturnMessage(LogLevel.Error, $"Failed to remove extracted subtitle stream: {subtitleStream.Identifier} from {SourcePath}"));
                }
            }
        }
        
        if (CacheState != CacheState.Cached)
        {
            Logger?.LogInformation("Media item is not cached: {SourcePath}", SourcePath);
            result.Messages.Add(new ReturnMessage(LogLevel.Information, "Media item is not cached"));
            result.Success = true;
            return result;
        }

        var cachePath = _dataService.CachePath;
        if (string.IsNullOrWhiteSpace(cachePath))
        {
            Logger?.LogError("Cache path is not set");
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Cache path is not set"));
            return result;
        }

        cachePath = Path.Combine(cachePath, PathTitle);
        if (!File.Exists(cachePath))
        {
            Logger?.LogWarning("Cache file does not exist: {Path}", cachePath);
            result.Messages.Add(new ReturnMessage(LogLevel.Warning, "Cache file does not exist: {Path}", cachePath));
            return result;
        }

        Logger?.LogDebug("Removing cache file: {Path}", cachePath);
        try
        {
            File.Delete(cachePath);
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to remove cache file: {Path}", cachePath);
            result.Messages.Add(new ReturnMessage(LogLevel.Error, $"Failed to remove cache file: {cachePath}"));
            return result;
        }
        
        Logger?.LogInformation("Removed cache file: {Path}", cachePath);
        CacheState = CacheState.NotCached;
        CacheProgress = 0;
        result.Success = true;
        return result;
    }

    public override ValueTask<ReturnResult<string>> GetCachePathAsync(CancellationToken cancellationToken = default)
    {
        var result = new ReturnResult<string>();
        var cachePath = _dataService.CachePath;
        if (string.IsNullOrWhiteSpace(cachePath))
        {
            Logger?.LogError("Cache path is not set");
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Cache path is not set"));
            return ValueTask.FromResult(result);
        }
        
        if (CacheState == CacheState.Caching)
        {
            Logger?.LogWarning("Media item is currently caching: {SourcePath}", SourcePath);
            result.Messages.Add(new ReturnMessage(LogLevel.Warning, "Media item is currently caching"));
            return ValueTask.FromResult(result);
        }
        
        var path = Path.Combine(cachePath, PathTitle);
        if (File.Exists(path))
        {
            Logger?.LogDebug("Cache file exists: {Path}", path);
            result.Success = true;
            result.Value = path;
            return ValueTask.FromResult(result);
        }

        Logger?.LogError("Cache file does not exist: {Path}", path);
        result.Messages.Add(new ReturnMessage(LogLevel.Error, "Cache file does not exist: {Path}", path));
        //reset cache state if its set to cached
        if (CacheState == CacheState.Cached)
        {
            CacheState = CacheState.NotCached;
            CacheProgress = 0;
        }
        return ValueTask.FromResult(result);
    }

    public override ValueTask<ReturnResult<Stream>> GetStreamAsync(CancellationToken cancellationToken = default)
    {
        var result = new ReturnResult<Stream>();
        if (CacheState != CacheState.Cached)
        {
            Logger?.LogError("Media item is not cached: {SourcePath}", SourcePath);
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Media item is not cached"));
            return ValueTask.FromResult(result);
        }

        var cachePath = _dataService.CachePath;
        if (string.IsNullOrWhiteSpace(cachePath))
        {
            Logger?.LogError("Cache path is not set");
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Cache path is not set"));
            return ValueTask.FromResult(result);
        }

        cachePath = Path.Combine(cachePath, PathTitle);
        if (!File.Exists(cachePath))
        {
            Logger?.LogError("Cache file does not exist: {Path}", cachePath);
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Cache file does not exist: {Path}", cachePath));
            return ValueTask.FromResult(result);
        }

        try
        {
            var stream = File.OpenRead(cachePath);
            result.Value = stream;
            result.Success = true;
            return ValueTask.FromResult(result);
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to open cache file: {Path}", cachePath);
            result.Messages.Add(new ReturnMessage(LogLevel.Error, $"Failed to open cache file: {cachePath}"));
            return ValueTask.FromResult(result);
        }
    }
}