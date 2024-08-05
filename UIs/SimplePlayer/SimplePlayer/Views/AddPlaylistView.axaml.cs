using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SimplePlayer.Models;

namespace SimplePlayer.Views;

public partial class AddPlaylistView : Window
{
    private PlaylistModel _playlist;

    public AddPlaylistView(PlaylistModel? playlist = null)
    {
        _playlist = playlist == null ? new PlaylistModel(string.Empty) : new PlaylistModel(playlist);
        InitializeComponent();
        PlaylistName.Text = _playlist.Name;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        PlaylistName.Focus();
        PlaylistName.SelectAll();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Enter)
            SaveButton_OnClick(null, null!);
        else if (e.Key == Key.Escape)
            CancelButton_OnClick(null, null!);
    }

    private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PlaylistName.Text)) 
            CancelButton_OnClick(sender, e);
        _playlist.Name = PlaylistName.Text!;
        this.Close(_playlist);
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        this.Close(null);
    }
}