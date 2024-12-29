using CommunityToolkit.Mvvm.ComponentModel;
using Manager2.Shared.Entities;
using Manager2.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace Manager2.Shared.BaseModels;

public abstract partial class MediaChannel : ObservableObject, IAsyncDisposable
{
    public PlaybackBackend Backend { get; private set; }

    public MediaItem Media { get; private set; }

    [ObservableProperty] private TimeSpan? _length;

    private TimeSpan _position;
    
    public TimeSpan Position
    {
        get => _position;
        protected set
        {
            if (value.Equals(_position)) return;
            var oldPosition = Position;
            OnPropertyChanging();
            OnPositionChanging(value);
            OnPositionChanging(oldPosition, value);
            _position = value;
            OnPropertyChanged();
            OnPositionChanged(value);
            OnPositionChanged(oldPosition, value);
        }
    }

    private ChannelState _state;

    public ChannelState State
    {
        get => _state;
        protected set
        {
            if (value == _state) return;
            var oldState = State;
            OnPropertyChanging();
            OnStateChanging(value);
            OnStateChanging(oldState, value);
            _state = value;
            OnPropertyChanged();
            OnStateChanged(value);
            OnStateChanged(oldState, value);
        }
    }
    
    
    protected ILogger<MediaChannel>? Logger { get; }
    
    protected MediaChannel(PlaybackBackend backend, MediaItem media, ILogger<MediaChannel>? logger = default)
    {
        Backend = backend;
        Media = media;
        Logger = logger;
    }

    public abstract ValueTask<ReturnResult> PlayAsync();
    
    public abstract ValueTask<ReturnResult> PauseAsync();
    
    public abstract ValueTask<ReturnResult> ResumeAsync();
    
    public abstract ValueTask<ReturnResult> StopAsync();
    
    public abstract ValueTask<ReturnResult> SetPositionAsync(TimeSpan position);
    
    public abstract ValueTask<ReturnResult> SetStateAsync(ChannelState state);

    public virtual ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
    
    partial void OnStateChanged(ChannelState state);
    partial void OnStateChanged(ChannelState oldValue, ChannelState newValue);
    partial void OnStateChanging(ChannelState state);
    partial void OnStateChanging(ChannelState oldValue, ChannelState newValue);
    partial void OnPositionChanged(TimeSpan position);
    partial void OnPositionChanged(TimeSpan oldValue, TimeSpan newValue);
    partial void OnPositionChanging(TimeSpan position);
    partial void OnPositionChanging(TimeSpan oldValue, TimeSpan newValue);
}