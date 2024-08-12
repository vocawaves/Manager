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
using Microsoft.Extensions.Logging;

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
        var logFactory = LoggerFactory.Create(opt => opt.AddConsole());
        var cm = new ComponentManager(logFactory);
        var localBackend = cm.CreateComponent<LocalDataService>("LocalBackend", 0);
        if (localBackend == null)
            return;
        var ffBackend = cm.CreateComponent<FFMPEGBackend>("FFMPEGBackend", 0);
        if (ffBackend == null)
            return;

        var could = await ffBackend.InitializeAsync();
        if (!could)
            return;

        var testPath = "C:\\Users\\Sekoree\\Videos\\07hxZ6aQhSaaVdyu.mp4";
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