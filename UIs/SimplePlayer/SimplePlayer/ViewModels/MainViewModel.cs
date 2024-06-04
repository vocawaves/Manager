using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Manager.MediaBackends.UI.LibVLCPlayer.Avalonia;
using Manager.Shared;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces.Video;
using Manager.SimplePlayer;
using Microsoft.Extensions.Logging;
using SimplePlayer.Models;
using SimplePlayer.Utilities;
using SimplePlayer.Views;

namespace SimplePlayer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel>? _logger;
    private readonly ComponentManager _componentManager;
    private readonly PlaylistIO? _playlistIO;
    public MediaPlayer Player { get; }
    public ObservableCollection<PlaylistModel> Playlists { get; } = new();

    [ObservableProperty] private PlaylistModel _selectedPlaylist;
    [ObservableProperty] private PlaylistItemModel? _selectedPlaylistItem;
    [ObservableProperty] private bool _canAddToPlaylist;

    [ObservableProperty] private TimeSpan _jumpToTime = TimeSpan.Zero;

    [ObservableProperty] private TimeSpan _remainingTime = TimeSpan.Zero;

    public IExternalPlayerSurface? VideoControl => Player.VideoSurface;
    private VideoPlayerWindow? _videoPlayerWindow;
    [ObservableProperty] private bool _canOpenVideoPlayer = true;
    [ObservableProperty] private bool _videoPlayerVisible = false;
    [ObservableProperty] private Bitmap? _videoPlayerWindowBackground;

    [ObservableProperty] private bool _controlsOnTop = false;
    [ObservableProperty] private bool _videoOnTop = false;

    public MainViewModel(ComponentManager componentManager, MediaPlayer player)
    {
        _componentManager = componentManager;
        _playlistIO = componentManager.CreateComponent<PlaylistIO>("PlaylistIO", 0);
        Player = player;
        Player.PlaybackPositionChanged += PlayerOnPlaybackPositionChanged;
        Player.PlaybackEnded += async (sender, args) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                RemainingTime = TimeSpan.Zero;
                VideoPlayerVisible = false;
            });
        };
        Player.PlaybackStopped += async (sender, args) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                RemainingTime = TimeSpan.Zero;
                VideoPlayerVisible = false;
            });
        };
        _logger = componentManager.CreateLogger<MainViewModel>();
        var scratchList = new PlaylistModel("Scratch List", false)
        {
            ParentCollection = Playlists
        };
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

    partial void OnVideoOnTopChanged(bool value)
    {
        if (_videoPlayerWindow == null)
            return;
        _videoPlayerWindow.Topmost = value;
    }

    #region Playlist Management

    public async Task AddPlaylist(Window parent)
    {
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

    public async Task LoadPlaylist(Window parent)
    {
        if (_playlistIO == null)
            return;
        var filePickerOptions = new FilePickerOpenOptions()
        {
            Title = "Load Playlist",
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Playlist")
                {
                    Patterns = new[] { "*.m3u" }
                }
            },
            AllowMultiple = false
        };
        var files = await parent.StorageProvider.OpenFilePickerAsync(filePickerOptions);
        if (!files.Any())
            return;

        var file = files.First();
        var path = file.TryGetLocalPath();
        if (path == null)
            return;

        var playlist = await _playlistIO.LoadPlaylist(path);
        if (playlist == null)
        {
            _logger?.LogError("Failed to load playlist.");
            return;
        }

        playlist.ParentCollection = Playlists;
        Playlists.Add(playlist);
        SelectedPlaylist = playlist;
    }

    public async Task LoadAllPlaylists(Window parent)
    {
        if (_playlistIO == null)
            return;
        var filePickerOptions = new FilePickerOpenOptions()
        {
            Title = "Load Playlists",
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Playlist Collection")
                {
                    Patterns = new[] { "*.kvpf" }
                }
            }
        };
        var files = await parent.StorageProvider.OpenFilePickerAsync(filePickerOptions);
        if (!files.Any())
            return;

        var file = files.First();
        var path = file.TryGetLocalPath();
        if (path == null)
        {
            _logger?.LogError("Failed to get local path.");
            return;
        }
        
        var playlists = await _playlistIO.LoadPlaylistCollection(path);
        if (playlists == null)
        {
            _logger?.LogError("Failed to load playlists.");
            return;
        }
        
        foreach (var playlist in playlists)
        {
            playlist.ParentCollection = Playlists;
            Dispatcher.UIThread.InvokeAsync(() => Playlists.Add(playlist));
        }
    }

    public async Task SavePlaylist(Window parent)
    {
        var selectedPlaylist = SelectedPlaylist;
        var filePickerOptions = new FilePickerSaveOptions()
        {
            Title = "Save Playlist",
            SuggestedFileName = $"{selectedPlaylist.Name}.m3u",
            FileTypeChoices = new[] { new FilePickerFileType("Playlist") { Patterns = new[] { "*.m3u" } } }
        };
        var file = await parent.StorageProvider.SaveFilePickerAsync(filePickerOptions);
        if (file == null)
        {
            _logger?.LogInformation("User cancelled save dialog.");
            return;
        }

        var path = file.TryGetLocalPath();
        if (path == null)
        {
            _logger?.LogError("Failed to get local path.");
            return;
        }
        
        if (!await _playlistIO!.SavePlaylist(selectedPlaylist, path))
        {
            _logger?.LogError("Failed to save playlist.");
        }
    }

    public async Task SaveAllPlaylists(Window parent)
    {
        var nonScratchPlaylists = Playlists.Where(x => x.IsRemovable);
        if (Playlists.Count == 0)
        {
            _logger?.LogError("No playlists to save.");
            return;
        }

        var filePickerOptions = new FilePickerSaveOptions()
        {
            Title = "Save Playlists",
            SuggestedFileName = "Playlists.kvpf",
            FileTypeChoices = new[] { new FilePickerFileType("Playlist Collection") { Patterns = new[] { "*.kvpf" } } }
        };
        var file = await parent.StorageProvider.SaveFilePickerAsync(filePickerOptions);
        if (file == null)
        {
            _logger?.LogInformation("User cancelled save dialog.");
            return;
        }

        var path = file.TryGetLocalPath();
        if (path == null)
        {
            _logger?.LogError("Failed to get local path.");
            return;
        }

        if (!await _playlistIO!.SavePlaylistCollection(nonScratchPlaylists, path))
        {
            _logger?.LogError("Failed to save playlists.");
        }
    }
    
    public void ClearPlaylist()
    {
        SelectedPlaylist.PlaylistItems.Clear();
    }
    
    public async Task SaveScratch(Window parent)
    {
        var scratchList = Playlists.FirstOrDefault(x => !x.IsRemovable);
        if (scratchList == null)
        {
            _logger?.LogError("No scratch list found.");
            return;
        }
        
        //Needs Add Playlist Dialog
    }

    #endregion

    #region Playback Controls

    public async Task Play()
    {
        if (SelectedPlaylistItem == null)
            return;

        VideoPlayerVisible = SelectedPlaylistItem.Item.ItemType is ItemType.Video or ItemType.Image;
        await Player.PlayAsync(SelectedPlaylistItem.Item);
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

    public async Task Stop()
    {
        await Player.StopAsync();
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
            DataContext = this,
            Topmost = VideoOnTop
        };
        _videoPlayerWindow.Closed += (sender, args) =>
        {
            _videoPlayerWindow = null;
            Player.VideoSurface = null;
            CanOpenVideoPlayer = true;
        };
        _videoPlayerWindow.Show();
        CanOpenVideoPlayer = false;
    }

    public void FullscreenOrWindowed()
    {
        if (_videoPlayerWindow == null)
            return;
        _videoPlayerWindow.WindowState = _videoPlayerWindow.WindowState == WindowState.FullScreen
            ? WindowState.Normal
            : WindowState.FullScreen;
    }

    public async Task SetVideoBackground(Window parent)
    {
        var filePickerOptions = new FilePickerOpenOptions()
        {
            Title = "Set Video Background",
            AllowMultiple = false
        };
        var files = await parent.StorageProvider.OpenFilePickerAsync(filePickerOptions);
        if (!files.Any())
            return;

        var file = files.First();
        var path = file.TryGetLocalPath();
        if (path == null)
            return;

        VideoPlayerWindowBackground = new Bitmap(path);
    }

    public async Task DoJumpToTime()
    {
        if (Player.ActiveMediaChannel == null)
            return;

        await Player.ActiveMediaChannel.SetPositionAsync(JumpToTime);
    }

    #endregion
}