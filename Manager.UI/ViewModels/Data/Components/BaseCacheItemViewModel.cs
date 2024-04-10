using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.Data;

namespace Manager.UI.ViewModels.Data.Components;

public partial class BaseCacheItemViewModel : ViewModelBase
{
    [ObservableProperty] private MediaItem _backingMediaItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCached))]
    [NotifyPropertyChangedFor(nameof(IsCaching))]
    [NotifyPropertyChangedFor(nameof(IsCacheFailed))]
    [NotifyPropertyChangedFor(nameof(IsNotCached))]
    [NotifyPropertyChangedFor(nameof(DataMimeType))]
    private CacheState _cacheState;

    [ObservableProperty] private double _cacheProgress;

    public bool IsCached => CacheState == CacheState.Cached;
    public bool IsCaching => CacheState == CacheState.Caching;
    public bool IsCacheFailed => CacheState == CacheState.Failed;
    public bool IsNotCached => CacheState == CacheState.NotCached;

    public string? CustomName
    {
        get => BackingMediaItem.CustomTitle;
        set
        {
            BackingMediaItem.CustomTitle = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    public string? DataMimeType => BackingMediaItem.MimeType;

    public virtual string DisplayName => BackingMediaItem.CustomTitle ?? BackingMediaItem.PathTitle;

    public BaseCacheItemViewModel(MediaItem mediaItem)
    {
        BackingMediaItem = mediaItem;
        BackingMediaItem.CacheStateChanged += OnCacheStateChanged;
        BackingMediaItem.CacheProgressChanged += OnCacheProgressChanged;
    }

    private async ValueTask OnCacheProgressChanged(object sender, CacheProgressChangesEventArgs eventArgs)
        => await Dispatcher.UIThread.InvokeAsync(() => CacheProgress = eventArgs.Progress);

    private async ValueTask OnCacheStateChanged(object sender, CacheStateChangedEventArgs eventArgs)
        => await Dispatcher.UIThread.InvokeAsync(() => CacheState = eventArgs.State);

    public async Task CacheAsync()
        => await BackingMediaItem.CacheAsync();

    public async Task UnCacheAsync()
        => await BackingMediaItem.RemoveFromCacheAsync();

    #region Design Time

    public BaseCacheItemViewModel()
    {
        BackingMediaItem = null!;
    }

    #endregion
}