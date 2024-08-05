using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Manager.MediaBackends.BassPlayer;
using Manager.Shared;
using Manager.Shared.Extensions;
using Manager.Shared.Interfaces.Audio;
using Manager.Shared.Interfaces.Data;
using SimplePlayer.API;
using SimplePlayer.Entities;
using SimplePlayer.Models;

namespace SimplePlayer.ViewModels;

public partial class SoundBoardsViewModel : ViewModelBase
{
    private readonly MainViewModel _mainVm;
    public ObservableCollection<SoundBoardModel> SoundBoards { get; } = new ObservableCollection<SoundBoardModel>();

    [ObservableProperty] private SoundBoardModel? _selectedSoundBoard;
    
    private readonly SfxApi _sfxApi;

    public SoundBoardsViewModel(MainViewModel mainVm, ComponentManager componentManager)
    {
        _mainVm = mainVm;
        _sfxApi = new SfxApi(componentManager, this);
    }
    
    public async Task EnableApi()
    {
        await _sfxApi.StartApi(_mainVm.ApiPort + 1);
    }
    
    public async Task DisableApi()
    {
        await _sfxApi.StopApi();
    }

    public void AddSoundBoard()
    {
        var soundBoard = new SoundBoardModel(this);
        SoundBoards.Add(soundBoard);
        SelectedSoundBoard = soundBoard;
        SelectedSoundBoard.IsEditing = true;
        SoundBoards.CollectionChanged += SoundBoardsOnCollectionChanged;
    }

    private void SoundBoardsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Move) 
            return;
        foreach (var soundBoard in SoundBoards)
        foreach (var sound in soundBoard.Sounds)
            sound.UpdateIndex();
    }

    public async Task SaveSoundBoard(Window window)
    {
        if (SelectedSoundBoard is null)
            return;

        var saveDialogOptions = new FilePickerSaveOptions()
        {
            Title = "Save SoundBoard",
            SuggestedFileName = $"{SelectedSoundBoard.Name}.sndbrd",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("SoundBoard config (.sndbrd)")
                {
                    Patterns = new[] { "*.sndbrd" }
                }
            }
        };
        var saveDialog = await window.StorageProvider.SaveFilePickerAsync(saveDialogOptions);
        var path = saveDialog?.TryGetLocalPath();
        if (path is null)
            return;

        var soundBoard = SelectedSoundBoard;
        var boardAsEntity = soundBoard.ToEntity();
        var json = JsonSerializer.Serialize(boardAsEntity, BoardJsonContext.Default.Options);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task SaveSoundBoards(Window window)
    {
        var saveDialogOptions = new FilePickerSaveOptions()
        {
            Title = "Save SoundBoards",
            SuggestedFileName = "SoundBoards.sndbrds",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("SoundBoards config (.sndbrds)")
                {
                    Patterns = new[] { "*.sndbrds" }
                }
            }
        };
        var saveDialog = await window.StorageProvider.SaveFilePickerAsync(saveDialogOptions);
        var path = saveDialog?.TryGetLocalPath();
        if (path is null)
            return;

        var soundBoards = SoundBoards.Select(sb => sb.ToEntity()).ToArray();
        var json = JsonSerializer.Serialize(soundBoards, BoardJsonContext.Default.Options);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task LoadSoundBoards(Window window)
    {
        var openDialogOptions = new FilePickerOpenOptions()
        {
            Title = "Load SoundBoards",
            FileTypeFilter = new[]
            {
                new FilePickerFileType("SoundBoards config (.sndbrds)")
                {
                    Patterns = new[] { "*.sndbrds" }
                }
            }
        };
        var openDialog = await window.StorageProvider.OpenFilePickerAsync(openDialogOptions);
        var path = openDialog.FirstOrDefault()?.TryGetLocalPath();
        if (path is null)
            return;

        var json = await File.ReadAllTextAsync(path);
        var soundBoards = JsonSerializer.Deserialize<SoundBoard[]>(json, BoardJsonContext.Default.Options);
        if (soundBoards is null)
            return;

        var lds = ComponentManager.MainInstance?.Components.OfType<IFileSystemSource>().FirstOrDefault();
        if (lds is null)
            return;

        var bas = ComponentManager.MainInstance?.Components.OfType<BassBackend>().FirstOrDefault();
        if (bas is null)
            return;

        foreach (var board in soundBoards)
        {
            var soundBoard = new SoundBoardModel(this, board);
            foreach (var sound in board.Sounds.Where(x =>
                         !string.IsNullOrWhiteSpace(x.MediaPath) && File.Exists(x.MediaPath)))
            {
                try
                {
                    var mediaItem = await lds.GetMediaItemAsync(sound.MediaPath!);
                    if (mediaItem is null)
                        continue;
                    await mediaItem.CacheAsync();
                    var channel = await bas.CreateChannelAsync(mediaItem);
                    if (channel is not IAudioChannel audioChannel)
                        continue;
                    var soundModel =
                        soundBoard.Sounds.FirstOrDefault(x => x.Column == sound.Column && x.Row == sound.Row);
                    if (soundModel is null)
                        continue;
                    await Dispatcher.UIThread.InvokeAsync(() => soundModel.SetChannel(audioChannel));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            await Dispatcher.UIThread.InvokeAsync(() => SoundBoards.Add(soundBoard));
        }
    }

    public async Task LoadSoundBoard(Window window)
    {
        var openDialogOptions = new FilePickerOpenOptions()
        {
            Title = "Load SoundBoard",
            FileTypeFilter = new[]
            {
                new FilePickerFileType("SoundBoard config (.sndbrd)")
                {
                    Patterns = new[] { "*.sndbrd" }
                }
            }
        };
        var openDialog = await window.StorageProvider.OpenFilePickerAsync(openDialogOptions);
        var path = openDialog.FirstOrDefault()?.TryGetLocalPath();
        if (path is null)
            return;

        var json = await File.ReadAllTextAsync(path);
        var soundBoard = JsonSerializer.Deserialize<SoundBoard>(json, BoardJsonContext.Default.Options);
        if (soundBoard is null)
            return;

        var lds = ComponentManager.MainInstance?.Components.OfType<IFileSystemSource>().FirstOrDefault();
        if (lds is null)
            return;

        var bas = ComponentManager.MainInstance?.Components.OfType<BassBackend>().FirstOrDefault();
        if (bas is null)
            return;

        var soundBoardModel = new SoundBoardModel(this, soundBoard);
        foreach (var sound in soundBoard.Sounds.Where(x =>
                     !string.IsNullOrWhiteSpace(x.MediaPath) && File.Exists(x.MediaPath)))
        {
            try
            {
                var mediaItem = await lds.GetMediaItemAsync(sound.MediaPath!);
                if (mediaItem is null)
                    continue;
                await mediaItem.CacheAsync();
                var channel = await bas.CreateChannelAsync(mediaItem);
                if (channel is not IAudioChannel audioChannel)
                    continue;
                var soundModel =
                    soundBoardModel.Sounds.FirstOrDefault(x => x.Column == sound.Column && x.Row == sound.Row);
                if (soundModel is null)
                    continue;
                await Dispatcher.UIThread.InvokeAsync(() => soundModel.SetChannel(audioChannel));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            SoundBoards.Add(soundBoardModel);
            SelectedSoundBoard ??= soundBoardModel;
        });
    }
}