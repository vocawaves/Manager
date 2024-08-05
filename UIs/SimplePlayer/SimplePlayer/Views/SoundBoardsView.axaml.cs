using Avalonia.Controls;
using Avalonia.Threading;
using SimplePlayer.ViewModels;

namespace SimplePlayer.Views;

public partial class SoundBoardsView : Window
{
    public SoundBoardsView()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        var dataContext = DataContext as SoundBoardsViewModel;
        if (dataContext is null)
            return;

        foreach (var board in dataContext.SoundBoards)
        foreach (var sound in board.Sounds)
            Dispatcher.UIThread.Invoke(async () =>
            {
                if (sound.Channel is not null)
                    await sound.Channel.DisposeAsync();
            });
    }
}