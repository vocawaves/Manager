using Manager2.Shared.BaseModels;
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

    public override async ValueTask<bool> CacheAsync(IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var cachePath = _dataService.CachePath;
        if (string.IsNullOrWhiteSpace(cachePath))
        {
            Logger?.LogError("Cache path is not set");
            return false;
        }

        cachePath = Path.Combine(cachePath, PathTitle);
        if (File.Exists(cachePath))
        {
            Logger?.LogDebug("Cache file already exists: {Path}", cachePath);
            if (progress != null)
                progress.Report(1);
            if (CacheState != CacheState.Cached)
                CacheState = CacheState.Cached;
            return true;
        }

        Logger?.LogDebug("Caching file: {SourcePath} to {CachePath}", SourcePath, cachePath);
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
        
        Logger?.LogInformation("Cached file: {SourcePath} to {CachePath}", SourcePath, cachePath);
        progress?.Report(1);
        CacheState = CacheState.Cached;
        return true;
    }

    public override async ValueTask<bool> CacheAsync(IProgress<double>? progress = null,
        CancellationToken cancellationToken = default,
        params MediaStream[] streamsToCache)
    {
        if (streamsToCache.Length == 0)
        {
            Logger?.LogWarning("No streams to cache");
            return false;
        }
        var failedStreams = new List<MediaStream>();
        foreach (var mediaStream in streamsToCache)
        {
            Logger?.LogDebug("Extracting stream: {Stream} from {SourcePath}", mediaStream.Identifier, SourcePath);
            var couldExtract = await mediaStream.ExtractStreamAsync(progress, cancellationToken);
            if (!couldExtract)
            {
                Logger?.LogError("Failed to extract stream: {Stream} from {SourcePath}", mediaStream.Identifier, SourcePath);
                failedStreams.Add(mediaStream);
                continue;
            }
            Logger?.LogInformation("Extracted stream: {Stream} from {SourcePath}", mediaStream.Identifier, SourcePath);
        }

        if (failedStreams.Count == 0) 
            return true;
        Logger?.LogError("Failed to extract {Count} streams from {SourcePath}", failedStreams.Count, SourcePath);
        return false;
    }

    public override async ValueTask<bool> RemoveCacheAsync(bool removeExtractedStreams = true,
        CancellationToken cancellationToken = default)
    {
        if (CacheState != CacheState.Cached && CacheState != CacheState.Caching && removeExtractedStreams == false)
        {
            Logger?.LogDebug("Media item is not cached: {SourcePath}", SourcePath);
            return true;
        }
        
        if (removeExtractedStreams)
        {
            foreach (var videoStream in VideoStreams)
            {
                if (videoStream.ExtractState != ExtractState.Extracted) 
                    continue;
                Logger?.LogDebug("Removing extracted video stream: {Stream} from {SourcePath}", videoStream.Identifier, SourcePath);
                var removed = await videoStream.RemoveExtractedStreamAsync(cancellationToken);
                if (removed)
                    Logger?.LogInformation("Removed extracted video stream: {Stream} from {SourcePath}", videoStream.Identifier, SourcePath);
                else
                    Logger?.LogError("Failed to remove extracted video stream: {Stream} from {SourcePath}", videoStream.Identifier, SourcePath);
            }
            foreach (var audioStream in AudioStreams)
            {
                if (audioStream.ExtractState != ExtractState.Extracted) 
                    continue;
                Logger?.LogDebug("Removing extracted audio stream: {Stream} from {SourcePath}", audioStream.Identifier, SourcePath);
                var removed = await audioStream.RemoveExtractedStreamAsync(cancellationToken);
                if (removed)
                    Logger?.LogInformation("Removed extracted audio stream: {Stream} from {SourcePath}", audioStream.Identifier, SourcePath);
                else
                    Logger?.LogError("Failed to remove extracted audio stream: {Stream} from {SourcePath}", audioStream.Identifier, SourcePath);
            }
            foreach (var subtitleStream in SubtitleStreams)
            {
                if (subtitleStream.ExtractState != ExtractState.Extracted) 
                    continue;
                Logger?.LogDebug("Removing extracted subtitle stream: {Stream} from {SourcePath}", subtitleStream.Identifier, SourcePath);
                var removed = await subtitleStream.RemoveExtractedStreamAsync(cancellationToken);
                if (removed)
                    Logger?.LogInformation("Removed extracted subtitle stream: {Stream} from {SourcePath}", subtitleStream.Identifier, SourcePath);
                else
                    Logger?.LogError("Failed to remove extracted subtitle stream: {Stream} from {SourcePath}", subtitleStream.Identifier, SourcePath);
            }
        }
        
        if (CacheState != CacheState.Cached)
        {
            Logger?.LogDebug("Media item is not cached: {SourcePath}", SourcePath);
            return true;
        }

        var cachePath = _dataService.CachePath;
        if (string.IsNullOrWhiteSpace(cachePath))
        {
            Logger?.LogError("Cache path is not set");
            return false;
        }

        cachePath = Path.Combine(cachePath, PathTitle);
        if (!File.Exists(cachePath))
        {
            Logger?.LogDebug("Cache file does not exist: {Path}", cachePath);
            return true;
        }

        Logger?.LogDebug("Removing cache file: {Path}", cachePath);
        File.Delete(cachePath);
        Logger?.LogInformation("Removed cache file: {Path}", cachePath);
        return true;
    }

    public override ValueTask<string?> GetCachePathAsync(CancellationToken cancellationToken = default)
    {
        var cachePath = _dataService.CachePath;
        if (string.IsNullOrWhiteSpace(cachePath))
        {
            Logger?.LogError("Cache path is not set");
            return new ValueTask<string?>((string?)null);
        }
        var path = Path.Combine(cachePath, PathTitle);
        if (File.Exists(path))
            return new ValueTask<string?>(path);
        
        Logger?.LogError("Cache file does not exist: {Path}", path);
        //reset cache state if its set to cached
        if (CacheState == CacheState.Cached)
            CacheState = CacheState.NotCached;
        return new ValueTask<string?>((string?)null);
    }

    public override ValueTask<Stream?> GetStreamAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}