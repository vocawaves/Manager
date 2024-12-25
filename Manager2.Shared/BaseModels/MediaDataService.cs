using System.Collections.Frozen;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Manager2.Shared.Enums;

namespace Manager2.Shared.BaseModels;

public abstract partial class MediaDataService : ObservableObject
{
    [ObservableProperty]
    private string? _cachePath;
    
    public ObservableCollection<MediaItem> MediaItems { get; set; } = new();

    /// <summary>
    /// This might be useless idk
    /// </summary>
    public FrozenSet<string> AvailableMethods { get; protected set; } = FrozenSet<string>.Empty;

    public abstract ValueTask<MediaItem?> GetMediaItemByPathAsync(string path);
}