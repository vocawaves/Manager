using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SimplePlayer.Models;

public partial class PlaylistModel : ObservableObject
{
    [ObservableProperty] private string _name;
    
    public bool IsRemovable { get; }
    
    public ObservableCollection<PlaylistItemModel> PlaylistItems { get; } = new();

    private ObservableCollection<PlaylistModel> _parentCollection;
    
    public PlaylistModel(ObservableCollection<PlaylistModel> parentCollection, string name, bool isRemovable = true)
    {
        _parentCollection = parentCollection;
        _name = name;
        IsRemovable = isRemovable;
    }
    
    public void RemoveItem(PlaylistItemModel item)
    {
        PlaylistItems.Remove(item);
    }
    
    public void Remove()
    {
        _parentCollection.Remove(this);
    }
}