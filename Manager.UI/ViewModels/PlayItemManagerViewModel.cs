using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Manager.Services.Data;
using Manager.Shared.Entities;
using Manager.UI.Models;

namespace Manager.UI.ViewModels;

public partial class PlayItemManagerViewModel : ViewModelBase
{
    public ObservableCollection<FileDirectoryItem> Directories { get; set; } = new();
    public ObservableCollection<FileDirectoryItem> Files { get; set; } = new();
    
    public ObservableCollection<PlayItem> CachedPlayItems { get; set; } = new();
    
    private LocalDataService _localDataService = new("LDS_Test", 0);

    public PlayItemManagerViewModel()
    {
        this.DirectorySelectedCommand.Execute(new FileDirectoryItem()
        {
            FullPath = string.Empty,
            ShortName = "..",
            IsDirectory = true,
            PressedCommand = DirectorySelectedCommand
        });
    }
    
    [RelayCommand]
    private async Task DirectorySelected(FileDirectoryItem directory)
    {
        var wasDevRoot = false;
        if (directory.ShortName == "..")
        {
            var lastSlash = directory.FullPath.LastIndexOf(Path.DirectorySeparatorChar);
            if (lastSlash == -1)
            {
                directory.FullPath = null!;
                wasDevRoot = true;
            }
            else
            {
                var parentDir = directory.FullPath.Substring(0, lastSlash);
                directory.FullPath = parentDir;
            }
        }
        
        var dirs = await _localDataService.GetDirectoriesAsync(directory.FullPath);
        Directories.Clear();

        if (!wasDevRoot)
        {
            var backItem = new FileDirectoryItem
            {
                FullPath = directory.FullPath,
                ShortName = "..",
                IsDirectory = true,
                PressedCommand = DirectorySelectedCommand
            };
            Directories.Add(backItem);
        }
        
        foreach (var dir in dirs)
        {
            var dirItem = new FileDirectoryItem
            {
                FullPath = dir,
                ShortName = Path.GetFileName(dir) == string.Empty ? dir : Path.GetFileName(dir),
                IsDirectory = true,
                PressedCommand = DirectorySelectedCommand
            };
            Directories.Add(dirItem);
        }
        
        if (wasDevRoot)
            return;
        
        var files = await _localDataService.GetFilesAsync(directory.FullPath);
        Files.Clear();
        
        foreach (var file in files)
        {
            var fileItem = new FileDirectoryItem
            {
                FullPath = file,
                ShortName = Path.GetFileName(file) ?? "No Name",
                IsDirectory = false,
                PressedCommand = FileSelectedCommand
            };
            Files.Add(fileItem);
        }
    }
    
    [RelayCommand]
    private async Task FileSelected(FileDirectoryItem file)
    {
        if (CachedPlayItems.Any(item => item.OwnerPath == file.FullPath))
            return;
        var playItem = await _localDataService.GetPlayItemAsync(file.FullPath);
        if (playItem is null)
            return;
        
        CachedPlayItems.Add(playItem);
    }

    [RelayCommand]
    private async Task PlayItemSelected(PlayItem playItem)
    {
        CachedPlayItems.Remove(playItem);
        await this._localDataService.RemovePlayItemFromCacheAsync(playItem);
    }
}