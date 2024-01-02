using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using Manager.Services.Data;
using Manager.Services.VlcVideo;
using Manager.Shared.Entities;
using Manager.Shared.Interfaces;
using Manager.UI.Models;

namespace Manager.UI.ViewModels;

public partial class PlayItemManagerViewModel : ViewModelBase
{
    public ObservableCollection<FileDirectoryItem> Directories { get; set; } = new();
    public ObservableCollection<FileDirectoryItem> Files { get; set; } = new();
    
    public ObservableCollection<PlayItem> CachedPlayItems { get; set; } = new();

    private readonly List<IDataService> _dataServices = new();
    
    private readonly LibVlcVideoBackendService _videoBackendService = new("VLC_Test", 0);
    
    public MediaPlayer MediaPlayer => _videoBackendService.MediaPlayer!;

    public PlayItemManagerViewModel()
    {
        var mounts = DriveInfo.GetDrives();
        foreach (var mount in mounts)
        {
            var lds = new LocalDataService(mount.Name, $"LDS_{mount.Name}", 0);
            this._dataServices.Add(lds);
        }
        
        this.DirectorySelectedCommand.Execute(new FileDirectoryItem()
        {
            FullPath = string.Empty,
            ShortName = "..",
            IsDirectory = true,
            PressedCommand = DirectorySelectedCommand
        });
        
        _ = Task.Run(async () =>
        {
            await _videoBackendService.InitializeAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.OnPropertyChanged(nameof(MediaPlayer));
            });
        });
    }
    
    [RelayCommand]
    private async Task DirectorySelected(FileDirectoryItem directory)
    {
        if (directory.ShortName == ".." && directory.FullPath == string.Empty)
        {
            this.Directories.Clear();
            this.Files.Clear();
            foreach (var dService in this._dataServices)
            {
                var root = new FileDirectoryItem()
                {
                    FullPath = dService.MountName,
                    ShortName = dService.MountName,
                    IsDirectory = true,
                    PressedCommand = DirectorySelectedCommand
                };
                this.Directories.Add(root);
            }
            return;
        }
        
        var dataService = this._dataServices.FirstOrDefault(x => directory.FullPath.StartsWith(x.MountName));
        if (dataService is null)
            return; //Bruh how
        
        var directories = await dataService.GetDirectoriesAsync(directory.FullPath);
        var files = await dataService.GetFilesAsync(directory.FullPath);
        
        this.Directories.Clear();
        this.Files.Clear();
        
        var previous = Path.GetDirectoryName(directory.FullPath);
        var parent = new FileDirectoryItem()
        {
            FullPath = previous ?? string.Empty,
            ShortName = "..",
            IsDirectory = true,
            PressedCommand = DirectorySelectedCommand
        };
        this.Directories.Add(parent);
        
        foreach (var dir in directories)
        {
            var dirItem = new FileDirectoryItem()
            {
                FullPath = dir,
                ShortName = Path.GetFileName(dir),
                IsDirectory = true,
                PressedCommand = DirectorySelectedCommand
            };
            this.Directories.Add(dirItem);
        }
        
        foreach (var file in files)
        {
            var fileItem = new FileDirectoryItem()
            {
                FullPath = file,
                ShortName = Path.GetFileName(file) ?? file,
                IsDirectory = false,
                PressedCommand = FileSelectedCommand
            };
            this.Files.Add(fileItem);
        }
    }
    
    [RelayCommand]
    private async Task FileSelected(FileDirectoryItem file)
    {
        var dataService = this._dataServices.FirstOrDefault(x => file.FullPath.StartsWith(x.MountName));
        if (dataService is null)
            return; //Bruh how
        
        var playItem = await dataService.GetPlayItemAsync(file.FullPath);
        if (playItem is null)
            return;
        
        CachedPlayItems.Add(playItem);
    }

    [RelayCommand]
    private async Task PlayItemSelected(PlayItem playItem)
    {
        await playItem.AssociatedDataService.CachePlayItemAsync(playItem);
        var channel = await _videoBackendService.CreateChannelAsync(playItem);
        if (channel is null)
            return;
        
        var could = await _videoBackendService.PlayChannelAsync(channel);
        if (!could)
            return;
        
        //await this._videoBackendService.SetChannelPositionAsync(channel, TimeSpan.FromHours(1.835));
        //CachedPlayItems.Remove(playItem);
        //await this._localDataService.RemovePlayItemFromCacheAsync(playItem);
    }
}