using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Manager.Shared.Entities;
using Manager.UI.Extras.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Manager.UI.ViewModels.Data.Components;

public partial class AudioCacheItemViewModel : BaseCacheItemViewModel
{
    public string? Title => ((AudioItem) BackingMediaItem).Title;
    public string? Artist => ((AudioItem) BackingMediaItem).Artist;
    
    public TimeSpan Duration => ((AudioItem) BackingMediaItem).Duration;
    
    public byte[]? AlbumArt => ((AudioItem) BackingMediaItem).AlbumArt;
    public string? AlbumArtMimeType => ((AudioItem) BackingMediaItem).AlbumArtMimeType;

    public override string DisplayName => GetDisplayName();

    [ObservableProperty] private AudioPreviewPlayerViewModel _previewPlayerViewModel;

    private string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(CustomName))
            return CustomName;
        if (!string.IsNullOrWhiteSpace(((AudioItem) BackingMediaItem).Title) && !string.IsNullOrWhiteSpace(((AudioItem) BackingMediaItem).Artist))
            return $"{((AudioItem) BackingMediaItem).Title} - {((AudioItem) BackingMediaItem).Artist}";
        if (!string.IsNullOrWhiteSpace(((AudioItem) BackingMediaItem).Title))
            return ((AudioItem) BackingMediaItem).Title!;
        return BackingMediaItem.PathTitle;
    }

    public AudioCacheItemViewModel(AudioItem mediaItem) : base(mediaItem)
    {
        PreviewPlayerViewModel = new AudioPreviewPlayerViewModel(mediaItem);
    }

    #region Design Time

    public AudioCacheItemViewModel()
    {
        PreviewPlayerViewModel = null!;
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var demoDs = new LocalDataService.LocalDataService( "Local", 0);
            await demoDs.InitializeAsync().ConfigureAwait(true);
            var demoItem = await demoDs.GetAudioItemAsync("C:\\Users\\Sekoree\\Music\\Sghenny Madattak - Na Na Na (Frenchcore).flac").ConfigureAwait(true);
            if (demoItem == null)
                return;
            BackingMediaItem = demoItem;
            PreviewPlayerViewModel = new AudioPreviewPlayerViewModel(demoItem);
            this.OnPropertyChanged(nameof(DisplayName));
            this.OnPropertyChanged(nameof(DataMimeType));
            this.OnPropertyChanged(nameof(IsCached));
            this.OnPropertyChanged(nameof(IsCaching));
            this.OnPropertyChanged(nameof(IsCacheFailed));
            this.OnPropertyChanged(nameof(IsNotCached));
            this.OnPropertyChanged(nameof(CacheState));
            this.OnPropertyChanged(nameof(CacheProgress));
            this.OnPropertyChanged(nameof(Title));
            this.OnPropertyChanged(nameof(Artist));
            this.OnPropertyChanged(nameof(Duration));
            this.OnPropertyChanged(nameof(AlbumArt));
            this.OnPropertyChanged(nameof(AlbumArtMimeType));
        });
    }

    #endregion
}