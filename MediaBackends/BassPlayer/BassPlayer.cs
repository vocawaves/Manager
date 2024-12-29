using CommunityToolkit.Mvvm.ComponentModel;
using ManagedBass;
using Manager2.Shared;
using Manager2.Shared.BaseModels;
using Manager2.Shared.Entities;
using Manager2.Shared.Enums;
using Manager2.Shared.Interfaces;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Manager.MediaBackends;

public partial class BassPlayer : PlaybackBackend, INeedsInitialization
{
    [ObservableProperty] private bool _isInitialized = false;

    public BassPlayer(string name, ILogger<PlaybackBackend>? logger = default) : base(name, logger)
    {
    }

    public ValueTask<ReturnResult> InitializeAsync()
    {
        var result = new ReturnResult();
        if (IsInitialized)
        {
            Logger?.LogWarning("Already initialized");
            result.Messages.Add(new ReturnMessage(LogLevel.Warning, "Already initialized"));
            result.Success = true;
            return ValueTask.FromResult(result);
        }

        try
        {
            var couldInit = Bass.Init();
            if (!couldInit)
            {
                Logger?.LogError("Failed to initialize Bass: {0}", Bass.LastError);
                result.Messages.Add(new ReturnMessage(LogLevel.Error, $"Failed to initialize Bass: {Bass.LastError}"));
                return ValueTask.FromResult(result);
            }

            //try loading the plugins
            var pluginDirectory = Path.Combine(Directory.GetCurrentDirectory(), "BassPlugins");
            if (!Directory.Exists(pluginDirectory))
            {
                Logger?.LogWarning("No plugin directory found at {0}", pluginDirectory);
                result.Messages.Add(new ReturnMessage(LogLevel.Warning,
                    $"No plugin directory found at {pluginDirectory}"));
            }
            else
            {
                var plugins = Directory.GetFiles(pluginDirectory, "*.dll");
                foreach (var plugin in plugins)
                {
                    Logger?.LogDebug("Loading plugin {0}", plugin);
                    var plResult = Bass.PluginLoad(plugin);
                    if (plResult != 0)
                        continue;

                    Logger?.LogWarning("Failed to load plugin {0}: {1}", plugin, Bass.LastError);
                    result.Messages.Add(new ReturnMessage(LogLevel.Warning,
                        $"Failed to load plugin {plugin}: {Bass.LastError}"));
                }
            }

            IsInitialized = true;
            Logger?.LogInformation("Initialized Bass");
            result.Success = true;
            return ValueTask.FromResult(result);
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to initialize Bass: {0}", e.Message);
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "Failed to initialize Bass: {0}", e.Message));
            return ValueTask.FromResult(result);
        }
    }

    public override ValueTask<ReturnResult> IsMediaStreamSupportedAsync(MediaStream stream)
    {
        var result = new ReturnResult();
        //is extracted it will definitely be supported, otherwise check for default bass supported formats
        if (stream.ExtractState == ExtractState.Extracted)
        {
            Logger?.LogInformation("Stream is extracted, supported (since its WAV)");
            result.Messages.Add(new ReturnMessage(LogLevel.Information,
                "Stream is extracted, supported (since its WAV)"));
            result.Success = true;
            return ValueTask.FromResult(result);
        }

        if (stream.ExtractState != ExtractState.Extracted && stream.MediaItem.AudioStreams.Count > 1)
        {
            Logger?.LogError("Stream has multiple audio streams, BASS does not support this, stream must be extracted");
            result.Messages.Add(new ReturnMessage(LogLevel.Error,
                "Stream has multiple audio streams, BASS does not support this, stream must be extracted"));
            return ValueTask.FromResult(result);
        }

        Logger?.LogDebug("Checking if stream is supported by BASS");
        var supported = Bass.CreateStream(stream.MediaItem.SourcePath, 0, 0, BassFlags.Default | BassFlags.Decode);
        if (supported == 0)
        {
            Logger?.LogError("Stream is not supported: {0}", Bass.LastError);
            result.Messages.Add(new ReturnMessage(LogLevel.Error, $"Stream is not supported: {Bass.LastError}"));
            return ValueTask.FromResult(result);
        }

        Bass.StreamFree(supported);
        Logger?.LogDebug("Stream is supported");
        result.Success = true;
        return ValueTask.FromResult(result);
    }

    public override async ValueTask<ReturnResult<MediaChannel>> CreateMediaChannelAsync(MediaStream stream)
    {
        var result = new ReturnResult<MediaChannel>();
        string? pathToUse = null;
        if (stream.ExtractState == ExtractState.Extracted)
        {
            Logger?.LogDebug("Using extracted stream, since its available");
            var extractedPath = await stream.GetExtractedStreamPathAsync();
            result.Messages.AddRange(extractedPath.Messages);
            if (!extractedPath.Success)
            {
                Logger?.LogError("Failed to get extracted stream path");
                result.Messages.Add(new ReturnMessage(LogLevel.Error, "Failed to get extracted stream path"));
            }
        }

        if (pathToUse == null && stream.ExtractState != ExtractState.Extracted &&
            stream.MediaItem.AudioStreams.Count > 1)
        {
            Logger?.LogError("Stream has multiple audio streams, BASS does not support this, stream must be extracted");
            result.Messages.Add(new ReturnMessage(LogLevel.Error,
                "Stream has multiple audio streams, BASS does not support this, stream must be extracted"));
            return result;
        }

        if (stream.MediaItem.CacheState == CacheState.Cached)
        {
            Logger?.LogDebug("Stream is cached, using cache path");
            var cachePath = await stream.MediaItem.GetCachePathAsync();
            result.Messages.AddRange(cachePath.Messages);
            if (!cachePath.Success)
            {
                Logger?.LogError("Failed to get cache path");
                result.Messages.Add(new ReturnMessage(LogLevel.Error, "Failed to get cache path"));
            }
            else
            {
                pathToUse = cachePath.Value;
            }
        }

        if (pathToUse == null)
        {
            Logger?.LogWarning(
                "Stream is not cached (or extracting), since only one audio stream is present, using source path");
            result.Messages.Add(new ReturnMessage(LogLevel.Warning,
                "Stream is not cached (or extracted), since only one audio stream is present, using source path"));
            pathToUse = stream.MediaItem.SourcePath;
        }

        if (!File.Exists(pathToUse))
        {
            Logger?.LogError("File does not exist: {0}", pathToUse);
            result.Messages.Add(new ReturnMessage(LogLevel.Error, "File does not exist: {0}", pathToUse));
            return result;
        }

        Logger?.LogDebug("Creating stream from {0}", pathToUse);
        var channel = Bass.CreateStream(pathToUse, 0, 0, BassFlags.Default | BassFlags.Float);
        if (channel == 0)
        {
            Logger?.LogError("Failed to create stream: {0}", Bass.LastError);
            result.Messages.Add(new ReturnMessage(LogLevel.Error, $"Failed to create stream: {Bass.LastError}"));
            return result;
        }

        var mediaChannel = new BassChannel(channel, this, stream.MediaItem, LoggingHelper.CreateLogger<MediaChannel>());
        var channelInit = mediaChannel.InitEndEvent();
        result.Messages.AddRange(channelInit.Messages);
        if (!channelInit.Success)
        {
            Logger?.LogError("Failed to initialize end event for channel: {0}", channelInit.Messages[0].Message);
            result.Messages.Add(new ReturnMessage(LogLevel.Error,
                $"Failed to initialize end event for channel: {channelInit.Messages[0].Message}"));
            await mediaChannel.DisposeAsync();
            return result;
        }
        
        Logger?.LogDebug("Created media channel for stream");
        result.Value = mediaChannel;
        result.Success = true;
        return result;
    }
}