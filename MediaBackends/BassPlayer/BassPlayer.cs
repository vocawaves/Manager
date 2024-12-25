using CommunityToolkit.Mvvm.ComponentModel;
using ManagedBass;
using Manager2.Shared;
using Manager2.Shared.BaseModels;
using Manager2.Shared.Enums;
using Manager2.Shared.Interfaces;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Manager.MediaBackends;

public partial class BassPlayer : PlaybackBackend, INeedsInitialization
{
    [ObservableProperty]
    private bool _isInitialized = false;
    
    public BassPlayer(string name, ILogger<PlaybackBackend>? logger = default) : base(name, logger)
    {
    }
    
    public ValueTask<bool> InitializeAsync()
    {
        if (IsInitialized)
        {
            Logger?.LogWarning("Already initialized");
            return new ValueTask<bool>(true);
        }
        try
        {

            var couldInit = Bass.Init();
            if (!couldInit)
            {
                Logger?.LogError("Failed to initialize Bass: {0}", Bass.LastError);
                return new ValueTask<bool>(false);
            }

            //try loading the plugins
            var pluginDirectory = Path.Combine(Directory.GetCurrentDirectory(), "BassPlugins");
            if (!Directory.Exists(pluginDirectory))
            {
                Logger?.LogWarning("No plugin directory found at {0}", pluginDirectory);
            }
            else
            {
                var plugins = Directory.GetFiles(pluginDirectory, "*.dll");
                foreach (var plugin in plugins)
                {
                    Logger?.LogDebug("Loading plugin {0}", plugin);
                    var result = Bass.PluginLoad(plugin);
                    if (result == 0)
                    {
                        Logger?.LogError("Failed to load plugin {0}: {1}", plugin, Bass.LastError);
                    }
                }
            }

            IsInitialized = true;
            return new ValueTask<bool>(true);
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to initialize Bass");
            return new ValueTask<bool>(false);
        }
    }

    public override ValueTask<bool> IsMediaStreamSupportedAsync(MediaStream stream)
    {
        return new ValueTask<bool>(true);
    }

    public override async ValueTask<MediaChannel?> CreateMediaChannelAsync(MediaStream stream)
    {
        string? pathToUse = null;
        if (stream.ExtractState == ExtractState.Extracted)
        {
            Logger?.LogDebug("Using extracted stream, since its available");
            pathToUse = await stream.GetExtractedStreamPathAsync();
        }
        else if (stream.ExtractState != ExtractState.Extracted && stream.MediaItem.AudioStreams.Count > 1)
        {
            Logger?.LogWarning("Stream has multiple audio streams, BASS does not support this, stream must be extracted");
            return null;
        }
        else
        {
            if (stream.MediaItem.CacheState != CacheState.Cached)
            {
                Logger?.LogWarning("Stream is not cached (or extracting), since only one audio stream is present, using source path");
                pathToUse = stream.MediaItem.SourcePath;
            }
            else
            {
                Logger?.LogDebug("Stream is cached, using cache path");
                pathToUse = await stream.MediaItem.GetCachePathAsync();
            }
        }
        
        if (!File.Exists(pathToUse))
        {
            Logger?.LogError("File does not exist: {0}", pathToUse);
            return null;
        }
        
        Logger?.LogDebug("Creating stream from {0}", pathToUse);
        var channel = Bass.CreateStream(pathToUse, 0, 0, BassFlags.Default | BassFlags.Float);
        if (channel == 0)
        {
            Logger?.LogError("Failed to create stream: {0}", Bass.LastError);
            return null;
        }
        
        var mediaChannel = new BassChannel(channel, this, stream.MediaItem, LoggingHelper.CreateLogger<MediaChannel>());
        return mediaChannel;
    }

    public override ValueTask<MediaChannel?> CreateMediaChannelAsync(MediaItem item)
    {
        throw new NotImplementedException();
    }
}