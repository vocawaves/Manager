using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SimplePlayer.Models;

public partial class PlaylistModel : ObservableObject
{
    [ObservableProperty] private string _name;
    
    public bool IsRemovable { get; }
    
    public ObservableCollection<PlaylistItemModel> PlaylistItems { get; } = new();

    public ObservableCollection<PlaylistModel>? ParentCollection { get; set; }
    
    public PlaylistModel(string name, bool isRemovable = true)
    {
        _name = name;
        IsRemovable = isRemovable;
    }
    
    public void RemoveItem(PlaylistItemModel item)
    {
        PlaylistItems.Remove(item);
    }
    
    public void Remove()
    {
        ParentCollection?.Remove(this);
    }
}