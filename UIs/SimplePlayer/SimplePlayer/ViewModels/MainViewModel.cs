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
using SimplePlayer.API;
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

    [ObservableProperty] private string? _jumpToTime = TimeSpan.Zero.ToString();

    [ObservableProperty] private TimeSpan _remainingTime = TimeSpan.Zero;
    [ObservableProperty] private TimeSpan _duration = TimeSpan.Zero;

    public IExternalPlayerSurface? VideoControl => Player.VideoSurface;
    private VideoPlayerWindow? _videoPlayerWindow;
    [ObservableProperty] private bool _canOpenVideoPlayer = true;
    [ObservableProperty] private bool _videoPlayerVisible;
    [ObservableProperty] private Bitmap? _videoPlayerWindowBackground;

    public string CurrentMedia => Player.ActiveMediaChannel?.MediaItem.PathTitle ?? "No Media";

    [ObservableProperty] private bool _controlsOnTop;
    [ObservableProperty] private bool _videoOnTop;

    [ObservableProperty] private bool _apiEnabled;
    [ObservableProperty] private int _apiPort = 8869;
    private MediaPlayerApi? _api;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(RepeatModeString))]
    private int _repeatMode;

    private SoundBoardsViewModel _soundBoardsVm;

    public string RepeatModeString => RepeatMode switch
    {
        0 => "None",
        1 => "Repeat One",
        2 => "Repeat All",
        _ => "Unknown"
    };

    public PlaylistItemModel? CurrentItem { get; set; }

    public MainViewModel(ComponentManager componentManager, MediaPlayer player)
    {
        _componentManager = componentManager;
        _playlistIO = componentManager.CreateComponent<PlaylistIO>("PlaylistIO", 0);
        Player = player;
        Player.PlaybackPositionChanged += PlayerOnPlaybackPositionChanged;
        Player.PlaybackEnded += OnPlayerOnPlaybackEnded;
        Player.PlaybackStopped += async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                RemainingTime = TimeSpan.Zero;
                VideoPlayerVisible = false;
            });
        };
        Player.ActiveChannelChanged += async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() => OnPropertyChanged(nameof(CurrentMedia)));
        };
        _logger = componentManager.CreateLogger<MainViewModel>();
        var scratchList = new PlaylistModel("Scratch List", false)
        {
            ParentCollection = Playlists
        };
        Playlists.Add(scratchList);
        SelectedPlaylist = scratchList;
        _soundBoardsVm = new SoundBoardsViewModel(this, componentManager);
    }

    private async ValueTask OnPlayerOnPlaybackEnded(object o, EventArgs eventArgs)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            RemainingTime = TimeSpan.Zero;
            VideoPlayerVisible = false;
        });
        if (RepeatMode == 1)
            await Dispatcher.UIThread.InvokeAsync(async () => await Play());
        else if (RepeatMode == 2)
        {
            if (CurrentItem == null) return;
            var index = CurrentItem.Parent.PlaylistItems.IndexOf(CurrentItem);
            if (index == CurrentItem.Parent.PlaylistItems.Count - 1) index = -1;
            var lastSelected = SelectedPlaylistItem;
            await Dispatcher.UIThread.InvokeAsync(() => SelectedPlaylistItem = SelectedPlaylist.PlaylistItems[index + 1]);
            await Dispatcher.UIThread.InvokeAsync(async () => await Play());
            await Dispatcher.UIThread.InvokeAsync(() => SelectedPlaylistItem = lastSelected);
        }
    }

    private async ValueTask PlayerOnPlaybackPositionChanged(object sender, TimeSpan eventArgs)
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

    partial void OnApiEnabledChanged(bool value)
    {
        _ = value
            ? Task.Run(async () =>
            {
                _api = new MediaPlayerApi(this._componentManager, this);
                await _api.StartApi();
                await _soundBoardsVm.EnableApi();
            })
            : Task.Run(async () =>
            {
                if (_api != null)
                {
                    await _api.StopApi();
                    _api = null;
                }
                await _soundBoardsVm.DisableApi();
            });
    }

    #region Playlist Management

    public async Task AddPlaylist(Window parent)
    {
        var playlistDialog = new AddPlaylistView();
        var dialogResult = await playlistDialog.ShowDialog<PlaylistModel?>(parent);
        if (dialogResult == null)
        {
            _logger?.LogInformation("User cancelled add playlist dialog.");
            return;
        }

        dialogResult.ParentCollection = Playlists;
        Playlists.Add(dialogResult);
        SelectedPlaylist = dialogResult;
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

    public async Task<PlaylistItemModel?> AddFileToPlaylist(PlaylistModel playlist, string uri)
    {
        var existingItem = Player.StoredMedia.FirstOrDefault(x => x.SourcePath == uri);
        if (existingItem != null)
        {
            var item = new PlaylistItemModel(playlist, existingItem);
            await Dispatcher.UIThread.InvokeAsync(() => playlist.PlaylistItems.Add(item));
            return item;
        }

        var mediaItem = await Player.AddMediaAsync(uri);
        if (mediaItem == null)
        {
            _logger?.LogError("Failed to add media item.");
            return null;
        }

        var playlistItem = new PlaylistItemModel(playlist, mediaItem);
        await Dispatcher.UIThread.InvokeAsync(() => playlist.PlaylistItems.Add(playlistItem));
        return playlistItem;
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
            await Dispatcher.UIThread.InvokeAsync(() => Playlists.Add(playlist));
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

        var playlistDialog = new AddPlaylistView(scratchList);
        var dialogResult = await playlistDialog.ShowDialog<PlaylistModel?>(parent);
        if (dialogResult == null)
        {
            _logger?.LogInformation("User cancelled save dialog.");
            return;
        }

        Playlists.Add(dialogResult);
        SelectedPlaylist = dialogResult;
    }

    #endregion

    #region Playback Controls

    public async Task Play()
    {
        if (SelectedPlaylistItem == null)
            return;

        VideoPlayerVisible = SelectedPlaylistItem.Item.ItemType is ItemType.Video or ItemType.Image;
        CurrentItem = SelectedPlaylistItem;
        await Player.PlayAsync(SelectedPlaylistItem.Item);
        await Dispatcher.UIThread.InvokeAsync(() => Duration = Player.ActiveMediaChannel?.Length ?? TimeSpan.Zero);
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

    public void OpenVideoPlayer()
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
        
        Player.VideoSurface = vlcControl;
        _videoPlayerWindow = new VideoPlayerWindow()
        {
            DataContext = this,
            Topmost = VideoOnTop
        };
        _videoPlayerWindow.Closed += (_, _) =>
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

    public void ClearVideoBackground()
    {
        VideoPlayerWindowBackground = null;
    }

    public async Task DoJumpToTime()
    {
        if (Player.ActiveMediaChannel == null || JumpToTime == null)
            return;

        TimeSpan toSet;
        if (JumpToTime.Contains(':'))
            toSet = TimeSpan.Parse(JumpToTime);
        else if (JumpToTime.EndsWith('s'))
            toSet = TimeSpan.FromSeconds(double.Parse(JumpToTime.TrimEnd('s')));
        else if (JumpToTime.EndsWith('m'))
            toSet = TimeSpan.FromMinutes(double.Parse(JumpToTime.TrimEnd('m')));
        else if (JumpToTime.EndsWith('h'))
            toSet = TimeSpan.FromHours(double.Parse(JumpToTime.TrimEnd('h')));
        else
        {
            var tryParse = TimeSpan.TryParse(JumpToTime, out toSet);
            if (!tryParse)
                return;
        }

        await Player.ActiveMediaChannel.SetPositionAsync(toSet);
    }

    #endregion

    public void OpenSoundBoard()
    {
        var sb = new SoundBoardsView()
        {
            DataContext = _soundBoardsVm 
        };
        sb.Show();
    }
}