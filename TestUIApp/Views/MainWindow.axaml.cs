using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using FFMPEGPlayer;
using FFMPEGPlayer.Avalonia;
using Manager.DataBackends.Local;
using Manager.Shared;
using Manager.Shared.Entities;
using Manager.Shared.Extensions;

namespace TestUIApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override async void OnInitialized()
    {
        base.OnInitialized();
        var cm = new ComponentManager(null);
        var localBackend = cm.CreateComponent<LocalDataService>("LocalBackend", 0);
        if (localBackend == null)
            return;
        var ffBackend = cm.CreateComponent<FFMPEGBackend>("FFMPEGBackend", 0);
        if (ffBackend == null)
            return;

        var could = await ffBackend.InitializeAsync();
        if (!could)
            return;

        var testPath = "C:\\Users\\Sekoree\\Videos\\2023.02.09_20.37.34_Audiosurf2_mODt7wC5bd.mp4";
        var mediaItem = await localBackend.GetMediaItemAsync(testPath);
        if (mediaItem == null)
            return;
        could = await mediaItem.CacheAsync();
        if (!could)
            return;
        
        var testChannel = new FFMPEGChannel(ffBackend, mediaItem);
        await testChannel.PrepareAsync();
        var videoControl = new FFMPEGVideoControl(testChannel);
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            Content = videoControl;
            await Task.Delay(2000);
            await testChannel.PlayAsync();
        });
    }
}