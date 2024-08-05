using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SimplePlayer.ViewModels;

namespace SimplePlayer.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        //Add drag and drop handler
        this.AddHandler(DragDrop.DropEvent, FilesDropHandler);
    }

    private void FilesDropHandler(object? sender, DragEventArgs e)
    {
        var files = e.Data.GetFiles();
        if (files is null)
            return;
        
        var dc = this.DataContext as MainViewModel;
        if (dc is null)
            return;

        var currentPlaylist = dc.SelectedPlaylist;
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            foreach (var file in files)
            {
                var path = file.TryGetLocalPath();
                if (path is null)
                    continue;
                await dc.AddFileToPlaylist(currentPlaylist,path);
            }
        });
    }

    private void PlaylistTab_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        //find the textbox to edit the name, sender should be the textblock
        if (sender is not TextBlock textBlock)
            return;
        var parentStackPanel = textBlock.Parent as StackPanel;
        if (parentStackPanel is null)
            return;
        var textBox = parentStackPanel.Children.FirstOrDefault(c => c is TextBox) as TextBox;
        if (textBox is null)
            return;
        textBlock.IsVisible = false;
        textBox.IsVisible = true;
        textBox.Focus();
        textBox.SelectAll();
    }

    private void PlaylistNameEdit_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;
        if (e.Key == Key.Enter)
        {
            //Save the new name
            var parentStackPanel = textBox.Parent as StackPanel;
            if (parentStackPanel is null)
                return;
            var textBlock = parentStackPanel.Children.FirstOrDefault(c => c is TextBlock) as TextBlock;
            if (textBlock is null)
                return;
            textBox.IsVisible = false;
            textBlock.IsVisible = true;
        }
        else if (e.Key == Key.Escape)
        {
            //Cancel the edit
            CancelPlaylistNameEdit(textBox);
        }
    }

    private void PlaylistNameEdit_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;
        CancelPlaylistNameEdit(textBox);
    }
    
    private void CancelPlaylistNameEdit(TextBox textBox)
    {
        //Find the textblock to show the name again
        var parentStackPanel = textBox.Parent as StackPanel;
        if (parentStackPanel is null)
            return;
        var textBlock = parentStackPanel.Children.FirstOrDefault(c => c is TextBlock) as TextBlock;
        if (textBlock is null)
            return;
        textBox.IsVisible = false;
        textBlock.IsVisible = true;
    }

    private void PlaylistItem_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        var dc = this.DataContext as MainViewModel;
        if (dc is null)
            return;
        Dispatcher.UIThread.InvokeAsync(async () => await dc.Play());
    }
}
