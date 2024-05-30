﻿using Manager.DataBackends.Local;
using Manager.MediaBackends.BassPlayer;
using Manager.MediaBackends.LibVLCPlayer;
using Manager.Shared;
using Manager.Shared.Cache;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.General;
using Manager.Shared.Extensions;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.Audio;
using Manager.Shared.Interfaces.Data;
using Manager.Shared.Interfaces.General;
using Manager.Shared.Interfaces.Video;
using Microsoft.Extensions.Logging;

namespace Manager.SimplePlayer;

public class MediaPlayer : ManagerComponent
{
    private readonly ILogger<MediaPlayer>? _logger;
    private IMediaChannel? _activeMediaChannel;
    
    public event AsyncEventHandler? ActiveChannelChanged;
    public event AsyncEventHandler<MediaItem>? MediaAdded;
    public event AsyncEventHandler<MediaItem>? MediaRemoved;
    public event AsyncEventHandler? PlaybackStarted;
    public event AsyncEventHandler? PlaybackPaused;
    public event AsyncEventHandler? PlaybackResumed;
    public event AsyncEventHandler? PlaybackStopped;
    public event AsyncEventHandler? PlaybackEnded;
    public event AsyncEventHandler<TimeSpan>? PlaybackPositionChanged;
    
    public List<IBackendService> BackendServices { get; } = new();
    public List<IDataService> DataServices { get; } = new();
    
    public List<MediaItem> StoredMedia { get; } = new();

    public IMediaChannel? ActiveMediaChannel
    {
        get => _activeMediaChannel;
        private set
        {
            _activeMediaChannel = value; 
            ActiveChannelChanged?.InvokeAndForget(this, EventArgs.Empty);
        }
    }

    public MediaPlayer(ComponentManager componentManager, string name, ulong parent, IComponentConfiguration? configuration = null) 
        : base(componentManager, name, parent, configuration)
    {
        _logger = componentManager.CreateLogger<MediaPlayer>();
    }
    
    public async ValueTask<MediaItem?> AddMediaAsync(string uri, ItemType type = ItemType.Guess)
    {
        var dataService = DataServices.FirstOrDefault();
        if (dataService == null)
        {
            _logger?.LogError("No data service found.");
            return null;
        }
        
        var asLocalDataService = dataService as LocalDataService;
        if (asLocalDataService == null)
        {
            _logger?.LogError("Data service is not a LocalDataService.");
            return null;
        }
        
        var mediaItem = await asLocalDataService.GetMediaItemAsync(uri, type);
        if (mediaItem == null)
        {
            _logger?.LogError("Failed to get media item.");
            return null;
        }

        await mediaItem.CacheAsync();
        StoredMedia.Add(mediaItem);
        MediaAdded?.InvokeAndForget(this, mediaItem);
        return mediaItem;
    }
    
    public ValueTask<bool> RemoveMediaAsync(MediaItem mediaItem)
    {
        if (!StoredMedia.Contains(mediaItem))
        {
            return ValueTask.FromResult(false);
        }
        
        StoredMedia.Remove(mediaItem);
        MediaRemoved?.InvokeAndForget(this, mediaItem);
        return ValueTask.FromResult(true);
    }
    
    public override async ValueTask<bool> InitializeAsync(params string[] options)
    {
        if (Initialized)
            return true;
        
        _logger?.LogInformation("Initializing MediaPlayer...");
        
        var bassBackend = this.ComponentManager.CreateManagerComponent<BassBackend>("BASS", 0);
        if (bassBackend == null)
        {
            _logger?.LogError("Failed to create BassBackend.");
            return false;
        }
        var vlcBackend = this.ComponentManager.CreateManagerComponent<LibVLCBackend>("LibVLC", 0);
        if (vlcBackend == null)
        {
            _logger?.LogError("Failed to create LibVLCBackend.");
            return false;
        }
        BackendServices.Add(bassBackend);
        BackendServices.Add(vlcBackend);
        
        var localCacheStrategy = DummyCacheStrategy.Create(ComponentManager.CreateLogger<DummyCacheStrategy>());
        var localDataConfig = new LocalDataServiceConfiguration()
        {
            CacheStrategy = localCacheStrategy
        };
        var localDataService = ComponentManager.CreateManagerComponent<LocalDataService>("LocalData", 0, localDataConfig);
        if (localDataService == null)
        {
            _logger?.LogError("Failed to create LocalDataService.");
            return false;
        }
        DataServices.Add(localDataService);
        
        foreach (var backend in BackendServices)
        {
            _logger?.LogInformation($"Initializing backend: {backend.Name}");
            await backend.InitializeAsync();
        }
        foreach (var dataService in DataServices)
        {
            _logger?.LogInformation($"Initializing data service: {dataService.Name}");
            await dataService.InitializeAsync();
        }
        
        _logger?.LogInformation("MediaPlayer initialized successfully.");
        Initialized = true;
        this.OnInitSuccess(Name);
        return true;
    }
    
    public async ValueTask<bool> PlayAsync(MediaItem mediaItem)
    {
        if (ActiveMediaChannel != null)
        {
            TearDownEventHandlers(ActiveMediaChannel);
            await ActiveMediaChannel.DestroyAsync();
        }

        IBackendService? mediaBackend = null;
        if (mediaItem.ItemType == ItemType.Audio)
            mediaBackend = BackendServices.FirstOrDefault(x => x is IAudioBackendService and not IVideoBackendService);
        else if (mediaItem.ItemType == ItemType.Video)
            mediaBackend = BackendServices.FirstOrDefault(x => x is IVideoBackendService);
        if (mediaBackend == null)
        {
            _logger?.LogError("No audio backend found.");
            return false;
        }

        var mediaChannel = await mediaBackend.CreateChannelAsync(mediaItem);
        if (mediaChannel == null)
        {
            _logger?.LogError("Failed to create media channel.");
            return false;
        }
        
        ActiveMediaChannel = mediaChannel;
        SetUpEventHandlers(mediaChannel);
        var could = await mediaChannel.PlayAsync();
        return could;
    }
    
    public async ValueTask<bool> PauseAsync()
    {
        if (ActiveMediaChannel == null)
        {
            return false;
        }
        var could = await ActiveMediaChannel.PauseAsync();
        return could;
    }
    
    public async ValueTask<bool> ResumeAsync()
    {
        if (ActiveMediaChannel == null)
        {
            return false;
        }
        var could = await ActiveMediaChannel.ResumeAsync();
        return could;
    }
    
    public async ValueTask<bool> StopAsync()
    {
        if (ActiveMediaChannel == null)
        {
            return false;
        }
        var could = await ActiveMediaChannel.StopAsync();
        return could;
    }
    
    public async ValueTask<bool> SetPositionAsync(TimeSpan position)
    {
        if (ActiveMediaChannel == null)
        {
            return false;
        }
        var could = await ActiveMediaChannel.SetPositionAsync(position);
        return could;
    }
    
    public async ValueTask<bool> SetPositionAsync(double positionMs)
    {
        if (ActiveMediaChannel == null)
        {
            return false;
        }
        var could = await ActiveMediaChannel.SetPositionAsync(positionMs);
        return could;
    }
    
    public async ValueTask<TimeSpan> GetPositionAsync()
    {
        if (ActiveMediaChannel == null)
        {
            return TimeSpan.Zero;
        }
        var position = await ActiveMediaChannel.GetPositionAsync();
        return position ?? TimeSpan.Zero;
    }

    private void SetUpEventHandlers(IMediaChannel mediaChannel)
    {
        mediaChannel.Playing += OnMediaChannelOnPlaying;
        mediaChannel.Paused += OnMediaChannelOnPaused;
        mediaChannel.Resumed += OnMediaChannelOnResumed;
        mediaChannel.Stopped += OnMediaChannelOnStopped;
        mediaChannel.Ended += OnMediaChannelOnEnded;
        mediaChannel.PositionChanged += OnMediaChannelOnPositionChanged;
    }
    
    private void TearDownEventHandlers(IMediaChannel mediaChannel)
    {
        mediaChannel.Playing -= OnMediaChannelOnPlaying;
        mediaChannel.Paused -= OnMediaChannelOnPaused;
        mediaChannel.Resumed -= OnMediaChannelOnResumed;
        mediaChannel.Stopped -= OnMediaChannelOnStopped;
        mediaChannel.Ended -= OnMediaChannelOnEnded;
        mediaChannel.PositionChanged -= OnMediaChannelOnPositionChanged;
    }

    private ValueTask OnMediaChannelOnPositionChanged(object sender, ChannelPositionChangedEventArgs args)
    {
        PlaybackPositionChanged?.InvokeAndForget(this, args.Position);
        return ValueTask.CompletedTask;
    }

    private ValueTask OnMediaChannelOnEnded(object sender, EventArgs args)
    {
        PlaybackEnded?.InvokeAndForget(this, EventArgs.Empty);
        return ValueTask.CompletedTask;
    }

    private ValueTask OnMediaChannelOnStopped(object sender, EventArgs args)
    {
        PlaybackStopped?.InvokeAndForget(this, EventArgs.Empty);
        return ValueTask.CompletedTask;
    }

    private ValueTask OnMediaChannelOnResumed(object sender, EventArgs args)
    {
        PlaybackResumed?.InvokeAndForget(this, EventArgs.Empty);
        return ValueTask.CompletedTask;
    }

    private ValueTask OnMediaChannelOnPaused(object sender, EventArgs args)
    {
        PlaybackPaused?.InvokeAndForget(this, EventArgs.Empty);
        return ValueTask.CompletedTask;
    }

    private ValueTask OnMediaChannelOnPlaying(object sender, EventArgs args)
    {
        PlaybackStarted?.InvokeAndForget(this, EventArgs.Empty);
        return ValueTask.CompletedTask;
    }
}