using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Manager.MediaBackends.LibVLCPlayer;
using Manager.MediaBackends.UI.LibVLCPlayer.Avalonia;
using Manager.Shared;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces.Video;
using Manager.SimplePlayer;
using Microsoft.Extensions.Logging;
using SimplePlayer.Models;
using SimplePlayer.Views;

namespace SimplePlayer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel>? _logger;
    private readonly ComponentManager _componentManager;
    public MediaPlayer Player { get; }
    public ObservableCollection<PlaylistModel> Playlists { get; } = new();

    [ObservableProperty] private PlaylistModel _selectedPlaylist;
    [ObservableProperty] private PlaylistItemModel? _selectedPlaylistItem;
    [ObservableProperty] private bool _canAddToPlaylist;
    
    [ObservableProperty] private TimeSpan _jumpToTime = TimeSpan.Zero;

    [ObservableProperty] private TimeSpan _remainingTime = TimeSpan.Zero;

    public IExternalPlayerSurface? VideoControl => Player.VideoSurface;
    private VideoPlayerWindow? _videoPlayerWindow;

    public MainViewModel(ComponentManager componentManager, MediaPlayer player)
    {
        _componentManager = componentManager;
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

    public async Task PauseResume()
    {
        if (Player.ActiveMediaChannel == null)
            return;
        var state = await Player.ActiveMediaChannel.GetStateAsync();
        if (state is ChannelState.Paused or ChannelState.Stopped or ChannelState.Ended)
        {
            await Player.ResumeAsync();
        }
        else
        {
            await Player.PauseAsync();
        }
    }

    public async Task OpenVideoPlayer()
    {
        if (_videoPlayerWindow != null)
        {
            _videoPlayerWindow.Activate();
            return;
        }

        var vlcControl = _componentManager.CreateComponent<VLCVideoControl>("VLCVideoControl", 0);
        if (vlcControl == null)
        {
            _logger?.LogError("Failed to create VLCVideoControl.");
            return;
        }

        await vlcControl.InitializeAsync();
        Player.VideoSurface = vlcControl;
        _videoPlayerWindow = new VideoPlayerWindow()
        {
            DataContext = this
        };
        _videoPlayerWindow.Closed += (sender, args) =>
        {
            _videoPlayerWindow = null;
            Player.VideoSurface = null;
        };
        _videoPlayerWindow.Show();
    }

    public void FullscreenOrWindowed()
    {
        if (_videoPlayerWindow == null)
            return;
        _videoPlayerWindow.WindowState = _videoPlayerWindow.WindowState == WindowState.FullScreen
            ? WindowState.Normal
            : WindowState.FullScreen;
    }

    public async Task DoJumpToTime()
    {
        if (Player.ActiveMediaChannel == null)
            return;
        
        await Player.ActiveMediaChannel.SetPositionAsync(JumpToTime);
    }

    #endregion
}