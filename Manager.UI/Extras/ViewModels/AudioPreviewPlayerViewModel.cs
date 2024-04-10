using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.Data;
using Manager.Shared.Events.General;
using Manager.Shared.Interfaces.Audio;
using Manager.UI.ViewModels;

namespace Manager.UI.Extras.ViewModels;

public partial class AudioPreviewPlayerViewModel : ViewModelBase
{
    private readonly AudioItem _backingMediaItem;
    
    [ObservableProperty]
    private IAudioChannel? _audioChannel;
    
    [ObservableProperty]
    private bool _isPlaying;
    
    [ObservableProperty]
    private double _volume = 1.0;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PositionDouble))]
    private TimeSpan _position;
    
    public double PositionDouble
    {
        get => Position.TotalSeconds;
        set => DoSeeking(value);
    }

    private void DoSeeking(double value)
    {
        if (AudioChannel == null) 
            return;
        _ = Task.Run(async () => await this.AudioChannel.SetPositionAsync(TimeSpan.FromSeconds(value)));
    }

    public TimeSpan Duration => _backingMediaItem.Duration;
    
    #region Demo
    
    public AudioPreviewPlayerViewModel(AudioItem mediaItem)
    {
        _backingMediaItem = mediaItem;
        this._backingMediaItem.CacheStateChanged += OnCacheStateChanged;
    }

    partial void OnVolumeChanged(double value)
    {
        if (AudioChannel == null) 
            return;
        _ = Task.Run(async () => await this.AudioChannel.SetVolumeAsync((float)value));
    }

    private async ValueTask OnCacheStateChanged(object sender, CacheStateChangedEventArgs eventArgs)
    {
        if (eventArgs.State is not CacheState.NotCached || this.AudioChannel == null) 
            return;
        await this.AudioChannel.DestroyAsync();
        this.AudioChannel = null;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsPlaying = false;
            return Position = TimeSpan.Zero;
        });
    }

    public async Task PlayDemo()
    {
        if (AudioChannel == null)
        {
            var channel = await MainViewModel.GlobalAudioBackendService.CreateChannelAsync(_backingMediaItem);
            if (channel == null || channel is not IAudioChannel aChan)
                return;
            AudioChannel = aChan;
            await AudioChannel.SetVolumeAsync((float)Volume);
            AudioChannel.StateChanged += OnAudioChannelStateChanged;
            AudioChannel.PositionChanged += OnAudioChannelPositionChanged;
        }

        await AudioChannel.ResumeAsync();
    }
    
    private async ValueTask OnAudioChannelPositionChanged(object sender, ChannelPositionChangedEventArgs eventArgs)
    {
        await Dispatcher.UIThread.InvokeAsync(() => Position = eventArgs.Position);
    }

    private async ValueTask OnAudioChannelStateChanged(object sender, ChannelStateChangedEventArgs eventArgs)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            switch (eventArgs.State)
            {
                case ChannelState.Playing:
                    this.IsPlaying = true;
                    break;
                case ChannelState.Paused:
                    this.IsPlaying = false;
                    break;
                case ChannelState.Ended:
                case ChannelState.Stopped:
                    this.IsPlaying = false;
                    this.Position = TimeSpan.Zero;
                    break;
            }
        });
    }

    public async Task PauseDemo()
    {
        if (AudioChannel == null) 
            return;
        
        await AudioChannel.PauseAsync();
    }
    
    public async Task DestroyDemo()
    {
        if (AudioChannel == null) 
            return;
        
        AudioChannel.StateChanged -= OnAudioChannelStateChanged;
        AudioChannel.PositionChanged -= OnAudioChannelPositionChanged;
        await AudioChannel.DestroyAsync().ConfigureAwait(true);
        Position = TimeSpan.Zero;
        IsPlaying = false;
        AudioChannel = null;
    }

    #endregion

    #region Design Time

    public AudioPreviewPlayerViewModel()
    {
        _backingMediaItem = null!;
    }

    #endregion
}