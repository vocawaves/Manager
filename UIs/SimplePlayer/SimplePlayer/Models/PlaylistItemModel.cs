using CommunityToolkit.Mvvm.ComponentModel;
using Manager.Shared.Entities;

namespace SimplePlayer.Models;

public partial class PlaylistItemModel : ObservableObject
{
    private PlaylistModel _parent;
    public MediaItem Item { get; }
    
    public string Name => Item.PathTitle;

    public PlaylistItemModel(PlaylistModel parent, MediaItem item)
    {
        _parent = parent;
        Item = item;
    }
    
    public void Remove()
    {
        _parent.RemoveItem(this);
    }
}