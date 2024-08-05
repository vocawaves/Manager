using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Manager.Shared.Entities;

namespace SimplePlayer.Models;

public partial class PlaylistItemModel : ObservableObject
{
    public PlaylistModel Parent { get; }
    public MediaItem Item { get; }
    
    public bool HasBPM => !string.IsNullOrWhiteSpace(Bpm);
    [ObservableProperty]
    private string? _bpm;
    
    public string Name => Item.PathTitle;

    public PlaylistItemModel(PlaylistModel parent, MediaItem item)
    {
        Parent = parent;
        Item = item;
        _ = Task.Run(TryGetBPM);
    }
    
    private async Task TryGetBPM()
    {
        try
        {
            var bpm = await BPMReader.BPMReader.TryGetBPM(Item);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (bpm == null)
                    return;
                Bpm = bpm.Value.ToString("0.00");
                this.OnPropertyChanged(nameof(HasBPM));
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    public void Remove()
    {
        Parent.RemoveItem(this);
    }
}