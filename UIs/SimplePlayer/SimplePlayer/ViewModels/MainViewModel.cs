using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Manager.Shared;
using Manager.Shared.Enums;
using Manager.SimplePlayer;
using Microsoft.Extensions.Logging;
using SimplePlayer.Models;

namespace SimplePlayer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel>? _logger;
    public MediaPlayer Player { get; }
    public ObservableCollection<PlaylistModel> Playlists { get; } = new();
    
    [ObservableProperty] private PlaylistModel _selectedPlaylist;
    [ObservableProperty] private PlaylistItemModel? _selectedPlaylistItem;
    [ObservableProperty] private bool _canAddToPlaylist;
    
    [ObservableProperty] private TimeSpan _remainingTime = TimeSpan.Zero;

    public MainViewModel(ComponentManager componentManager, MediaPlayer player) 
    {
        Player = player;
        Player.PlaybackPositionChanged += PlayerOnPlaybackPositionChanged;
        _logger = componentManager.CreateLogger<MainViewModel>();
        var scratchList = new PlaylistModel(Playlists, "Scratch List", false);
        Playlists.Add(scratchList);
        SelectedPlaylist = scratchList;
    }

    private async ValueTask PlayerOnPlaybackPositionChanged(object sender, TimeSpan eventargs)
    {
        if (Player.ActiveMediaChannel == null)
            return;

        var remaining = Player.ActiveMediaChannel.Length - Player.ActiveMediaChannel.Position;
        await Dispatcher.UIThread.InvokeAsync(() => RemainingTime = remaining ?? TimeSpan.Zero);
    }

    #region Playlist Management 

    public async Task AddPlaylist(Window parent)
    {
        var pls = Playlists;
        var selectedPlaylist = SelectedPlaylist;
        await Task.CompletedTask;
    }
    
    public async Task AddToPlaylist(Window parent)
    {
        CanAddToPlaylist = false;
        var filePickerOptions = new FilePickerOpenOptions()
        {
            Title = $"Add to Playlist: {SelectedPlaylist.Name}",
            AllowMultiple = true
        };
        var files = await parent.StorageProvider.OpenFilePickerAsync(filePickerOptions);
        if (!files.Any())
        {
            CanAddToPlaylist = true;
            return;
        }

        var currentPlaylist = SelectedPlaylist;
        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (path == null)
                continue;
            
            await AddFileToPlaylist(currentPlaylist, path);
        }
        
        CanAddToPlaylist = true;
    }
    public async Task AddFileToPlaylist(PlaylistModel playlist, string uri)
    {
        var existingItem = Player.StoredMedia.FirstOrDefault(x => x.SourcePath == uri);
        if (existingItem != null)
        {
            var item = new PlaylistItemModel(playlist, existingItem);
            await Dispatcher.UIThread.InvokeAsync(() => playlist.PlaylistItems.Add(item));
            return;
        }

        var mediaItem = await Player.AddMediaAsync(uri);
        if (mediaItem == null)
        {
            _logger?.LogError("Failed to add media item.");
            return;
        }
        
        var playlistItem = new PlaylistItemModel(playlist, mediaItem);
        await Dispatcher.UIThread.InvokeAsync(() => playlist.PlaylistItems.Add(playlistItem));
    }
    
    public async Task SavePlaylist(Window parent)
    {
        var selectedPlaylist = SelectedPlaylist;
        await Task.CompletedTask;
    }
    
    public async Task SaveAllPlaylists(Window parent)
    {
        await Task.CompletedTask;
    }

    #endregion

    #region Playback Controls

    public async Task Play()
    {
        if (SelectedPlaylistItem == null)
            return;
        
        await Player.PlayAsync(SelectedPlaylistItem.Item);
        //await Player.PlayAsync(SelectedPlaylistItem.Item);
    }

    #endregion
}
