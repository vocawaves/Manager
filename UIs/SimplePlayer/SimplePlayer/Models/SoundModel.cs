using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Manager.MediaBackends.BassPlayer;
using Manager.Shared;
using Manager.Shared.Events.General;
using Manager.Shared.Extensions;
using Manager.Shared.Interfaces.Audio;
using Manager.Shared.Interfaces.Data;
using SimplePlayer.Entities;

namespace SimplePlayer.Models;

public partial class SoundModel : ObservableObject
{
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(Name))]
    [NotifyPropertyChangedFor(nameof(Duration))]
    [NotifyPropertyChangedFor(nameof(TimeTotal))]
    private IAudioChannel? _channel;
    
    public TimeSpan Duration => Channel?.Length ?? TimeSpan.Zero;

    public string Name => Channel?.MediaItem.PathTitle ?? "No Sound";

    //Soundboard Index/Position
    //Soundboard Index/Row/Colum
    public string IndexHelper => $"{Parent.Parent.SoundBoards.IndexOf(Parent)}/{VisualIndex}\n" +
                                 $"{Parent.Parent.SoundBoards.IndexOf(Parent)}/{Row}/{Column}";

    public int VisualIndex => Column + Row * Parent.BoardColumns;
    
    public double TimeRemaining => TimeTotal - TimeElapsed;
    
    public TimeSpan TimeRemainingSpan => Channel?.Length - Channel?.Position  ?? TimeSpan.Zero;
    
    public double TimeElapsed => Channel?.Position?.TotalSeconds ?? 0;
    
    public double TimeTotal => Channel?.Length?.TotalSeconds ?? 0;
    
    public double FadeElapsed => (Stopwatch.GetTimestamp() - _fadeStart) / (double)Stopwatch.Frequency;
    
    public double FadeRemaining => FadeDuration - FadeElapsed;
    
    public TimeSpan FadeRemainingSpan => TimeSpan.FromSeconds(FadeDuration - FadeElapsed);

    [ObservableProperty] private double _fadeDuration = 3.0;

    [ObservableProperty] private double _volume = 100.0;

    [ObservableProperty] private int _column = 0;

    [ObservableProperty] private int _row = 0;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(BackgroundColor))]
    private bool _isPlaying;

    [ObservableProperty] private bool _loop;

    [ObservableProperty] private bool _fade;
    
    public ObservableCollection<string> Devices { get; set; } = new();
    
    [ObservableProperty]
    private string? _selectedDevice;

    public IImmutableSolidColorBrush? BackgroundColor =>
        //Green if playing, yellow if fading, transparent if not playing
        IsFading ? new ImmutableSolidColorBrush(SolidColorBrush.Parse("#66FAB941")) :
        IsPlaying ? new ImmutableSolidColorBrush(SolidColorBrush.Parse("#66A4C098")) :
        new ImmutableSolidColorBrush(SolidColorBrush.Parse("#222222"));

    public SoundBoardModel Parent { get; set; }

    public SoundModel(SoundBoardModel parent)
    {
        Parent = parent;
        _ = Task.Run(async () =>
        {
            var backend = ComponentManager.MainInstance?.Components.OfType<BassBackend>().FirstOrDefault();
            if (backend is null)
                return;
            var devices = await backend.GetDevicesAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Devices.Clear();
                foreach (var device in devices)
                    Devices.Add(device.Name);
            });
        });
    }

    partial void OnVolumeChanged(double value)
    {
        if (Channel is null)
            return;
        _ = Task.Run(async () => await Channel.SetVolumeAsync((float)(value / 100.0)));
    }

    partial void OnSelectedDeviceChanged(string? value)
    {
        _ = Task.Run(async () =>
        {
            if (Channel is null)
                return;
            var audioBackend = Channel?.AssociatedBackend as BassBackend;
            if (audioBackend is null)
                return;
            var devices = await audioBackend.GetDevicesAsync();
            var defaultDevice = devices.FirstOrDefault(x => x.Name.Contains("default", StringComparison.OrdinalIgnoreCase));
            if (defaultDevice is null)
                return;
            if (value is null && value?.ToLower().Contains("default") == true)
            {
                await Channel!.SetDeviceAsync(defaultDevice);
                return;
            }
            var device = devices.FirstOrDefault(x => x.Name == value);
            if (device is not null)
                await Channel!.SetDeviceAsync(device);
        });
    }

    public void UpdateIndex()
    {
        this.OnPropertyChanged(nameof(IndexHelper));
    }

    public async Task SetChannel(IAudioChannel? channel)
    {
        if (this.Channel is not null) 
            await this.Channel.DisposeAsync();
        Channel = channel;
        if (Channel is null)
            return;
        Channel.PositionChanged += ChannelOnPositionChanged;
        Channel.Ended += ChannelOnEnded;
        Channel.Playing += ChannelOnPlaying;
        Channel.Stopped += ChannelOnStopped;
        await Channel.SetVolumeAsync((float)(Volume / 100.0));
        if (this.SelectedDevice is not null || this.Devices.Count > 0)
        {
            if (this.SelectedDevice?.Contains("default", StringComparison.OrdinalIgnoreCase) == true)
                return;
            var audioBackend = channel?.AssociatedBackend as BassBackend;
            if (audioBackend is null)
                return;
            var devices = await audioBackend.GetDevicesAsync();
            var device = devices.FirstOrDefault(x => x.Name == this.SelectedDevice);
            if (device is not null)
                await Channel.SetDeviceAsync(device);
        }
    }

    private async ValueTask ChannelOnStopped(object sender, EventArgs eventargs)
    {
        await Dispatcher.UIThread.InvokeAsync(() => IsPlaying = false);
    }

    private async ValueTask ChannelOnPlaying(object sender, EventArgs eventargs)
    {
        await Dispatcher.UIThread.InvokeAsync(() => IsPlaying = true);
    }


    [ObservableProperty] [NotifyPropertyChangedFor(nameof(BackgroundColor))]
    private bool _isFading = false;

    private long _fadeStart = 0;

    private async ValueTask ChannelOnPositionChanged(object sender, ChannelPositionChangedEventArgs e)
    {
        if (Channel is null)
            return;

        this.OnPropertyChanged(nameof(TimeElapsed));
        this.OnPropertyChanged(nameof(TimeRemaining));
        this.OnPropertyChanged(nameof(TimeRemainingSpan));
        this.OnPropertyChanged(nameof(FadeElapsed));
        this.OnPropertyChanged(nameof(FadeRemaining));
        this.OnPropertyChanged(nameof(FadeRemainingSpan));
        
        if (!IsFading)
            return;

        var elapsed = (Stopwatch.GetTimestamp() - _fadeStart) / (double)Stopwatch.Frequency;
        //if more than FadeDuration is elapsed, stop
        //if not just gradually lower the volume
        if (elapsed > FadeDuration)
        {
            await Channel.StopAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsFading = false;
                IsPlaying = false;
            });
        }
        else
        {
            //respect the volume setting
            var volume = (float)(Volume / 100.0);
            var fade = (float)(1 - elapsed / FadeDuration);
            await Channel.SetVolumeAsync(volume * fade);
        }
    }

    private async ValueTask ChannelOnEnded(object sender, EventArgs e)
    {
        if (Loop && !IsFading && Channel != null)
        {
            await Channel.SetVolumeAsync((float)(Volume / 100.0));
            await Channel.PlayAsync();
        }
        else
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsFading = false;
                IsPlaying = false;
            });
    }

    public async Task Trigger()
    {
        if (Channel is null)
            return;

        if ((IsFading && !Fade) || (!IsPlaying))
            IsFading = false;

        if (IsFading || (!Fade && IsPlaying))
        {
            await Channel.StopAsync();
            return;
        }

        if (Fade && !IsFading && IsPlaying)
        {
            IsFading = true;
            _fadeStart = Stopwatch.GetTimestamp();
        }
        else
        {
            IsPlaying = true;
            await Channel.SetVolumeAsync((float)(Volume / 100.0));
            await Channel.PlayAsync();
        }
    }

    public async Task Remove()
    {
        if (Channel is not null)
            await Channel.DisposeAsync();
        Channel = null;
    }

    public async Task LoadSound(Window window)
    {
        var openDialogOptions = new FilePickerOpenOptions()
        {
            Title = "Load Sound",
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Audio files")
                {
                    Patterns =
                        "*.mp3;*.wav;*.ogg;*.opus;*.flac;*.m4a;*.aac;*.wma;*.aiff;*.ape;*.alac;*.dsf;*.dff".Split(';')
                }
            }
        };
        var openDialog = await window.StorageProvider.OpenFilePickerAsync(openDialogOptions);
        var path = openDialog.FirstOrDefault()?.TryGetLocalPath();
        if (path is null)
            return;

        var lds = ComponentManager.MainInstance?.Components.OfType<IFileSystemSource>().FirstOrDefault();
        if (lds is null)
            return;

        var mediaItem = await lds.GetMediaItemAsync(path);
        if (mediaItem is null)
            return;

        await mediaItem.CacheAsync();

        var bas = ComponentManager.MainInstance?.Components.OfType<BassBackend>().FirstOrDefault();
        if (bas is null)
            return;

        var channel = await bas.CreateChannelAsync(mediaItem);
        if (channel is not IAudioChannel audioChannel)
            return;

        await Dispatcher.UIThread.InvokeAsync(() => SetChannel(audioChannel));
    }

    public Sound ToEntity()
    {
        return new Sound()
        {
            Column = Column,
            Row = Row,
            Loop = Loop,
            Fade = Fade,
            MediaPath = Channel?.MediaItem.SourcePath ?? null,
            FadeDuration = FadeDuration,
            Volume = Volume,
            Device = SelectedDevice
        };
    }
}