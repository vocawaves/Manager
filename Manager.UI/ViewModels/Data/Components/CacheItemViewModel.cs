using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.Data;
using Microsoft.Extensions.Logging;

namespace Manager.UI.ViewModels.Data.Components;

public partial class CacheItemViewModel : ViewModelBase
{
    [ObservableProperty] private MediaItem _backingMediaItem;

    [ObservableProperty] private double _cacheProgress;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(IsCached))]
    [NotifyPropertyChangedFor(nameof(IsCaching))]
    [NotifyPropertyChangedFor(nameof(IsCacheInitial))]
    [NotifyPropertyChangedFor(nameof(DataMimeType))]
    private CacheState _cacheState;

    public bool IsCached => CacheState == CacheState.Cached;
    public bool IsCaching => CacheState == CacheState.Caching;
    public bool IsCacheInitial => CacheState == CacheState.NotCached;

    [ObservableProperty] private string? _displayName;

    public string? DataMimeType => BackingMediaItem?.MimeType;

    public object? HighlightInfo => GetHighlightInfo();

    private object? GetHighlightInfo()
        => BackingMediaItem switch
        {
            AudioItem audioItem => audioItem.Duration,
            _ => null
        };

    public CacheItemViewModel(MediaItem backingMediaItem)
    {
        this.BackingMediaItem = backingMediaItem;
        CacheProgress = backingMediaItem.CacheProgress;
        CacheState = backingMediaItem.CacheState;
        DisplayName = backingMediaItem.DisplayTitle;
        this.BackingMediaItem.CacheProgressChanged += this.BackingMediaItemOnCacheProgressChanged;
        this.BackingMediaItem.CacheStateChanged += this.BackingMediaItemOnCacheStateChanged;
    }

    partial void OnDisplayNameChanged(string? value)
        => this.BackingMediaItem.CustomTitle = value;

    private async ValueTask BackingMediaItemOnCacheStateChanged(object sender, CacheStateChangedEventArgs eventArgs)
        => await Dispatcher.UIThread.InvokeAsync(() => CacheState = eventArgs.State);

    private async ValueTask BackingMediaItemOnCacheProgressChanged(object sender,
        CacheProgressChangesEventArgs eventArgs)
        => await Dispatcher.UIThread.InvokeAsync(() => CacheProgress = eventArgs.Progress);

    public async Task CacheItem()
    {
        await this.BackingMediaItem.CacheAsync();
    }

    public async Task Delete()
    {
        await this.BackingMediaItem.RemoveFromCacheAsync();
    }

    #region Design Time

    /// <summary>
    /// Design time constructor. Do not use.
    /// </summary>
    public CacheItemViewModel()
    {
        this.BackingMediaItem = null!;
        Dispatcher.UIThread.InvokeAsync(InitDemoData);
    }

    private async ValueTask InitDemoData()
    {
        var lf = new LoggerFactory();
        var dataService = new LocalDataService.LocalDataService(lf, "Test", 0);
        var demoItemPath = "C:\\Users\\Sekoree\\Music\\Synthesizer V ANRI, AUDIOLOGIE - Now's the Time.flac";
        var item = await dataService.GetAudioItemAsync(demoItemPath).ConfigureAwait(true);
        if (item == null) return;
        this.BackingMediaItem = item;
        this.BackingMediaItem.CacheProgressChanged += this.BackingMediaItemOnCacheProgressChanged;
        this.BackingMediaItem.CacheStateChanged += this.BackingMediaItemOnCacheStateChanged;
        this.OnPropertyChanged(nameof(BackingMediaItem));
        this.OnPropertyChanged(nameof(HighlightInfo));
        CacheProgress = BackingMediaItem.CacheProgress;
        CacheState = BackingMediaItem.CacheState;
        DisplayName = BackingMediaItem.DisplayTitle;
    }

    #endregion
}