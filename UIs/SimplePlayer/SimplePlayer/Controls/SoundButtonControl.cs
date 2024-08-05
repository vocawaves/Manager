using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Manager.Shared;
using Manager.Shared.Enums;
using Manager.Shared.Extensions;
using Manager.Shared.Interfaces.Audio;
using Manager.Shared.Interfaces.Data;
using SimplePlayer.Models;

namespace SimplePlayer.Controls;

public class SoundButtonControl : UserControl
{
    public SoundButtonControl()
    {
        this.AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var file = e.Data.GetFiles();
        if (file is null)
            return;
        
        var firstFile = file.FirstOrDefault();
        if (firstFile is null)
            return;
        
        var path = firstFile.TryGetLocalPath();
        if (path is null || !File.Exists(path))
            return;
        
        Dispatcher.UIThread.InvokeAsync(async () => await HandleDroppedFiles(path));
    }

    private async Task HandleDroppedFiles(string path)
    {
        try
        { 
            var dc = this.DataContext as SoundModel;
            if (dc is null)
                return;

            var lds = ComponentManager.MainInstance?.Components.OfType<IFileSystemSource>().FirstOrDefault();
            if (lds is null)
                return;

            var bas = ComponentManager.MainInstance?.Components.OfType<IAudioBackendService>().FirstOrDefault();
            if (bas is null)
                return;

            var mediaItem = await lds.GetMediaItemAsync(path);
            if (mediaItem is null || mediaItem.ItemType != ItemType.Audio)
                return;

            await mediaItem.CacheAsync();

            var channel = await bas.CreateChannelAsync(mediaItem);
            if (channel is null)
                return;

            if (channel is not IAudioChannel audioChannel)
                return;
            
            await Dispatcher.UIThread.InvokeAsync(() => dc.SetChannel(audioChannel));
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }
}