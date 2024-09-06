using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FFMPEGPlayer;
using FFMPEGPlayer.Avalonia;
using Manager.DataBackends.Local;
using Manager.MediaBackends.BassPlayer;
using Manager.Shared;
using Manager.Shared.Extensions;
using Manager.Shared.Interfaces.General;
using Microsoft.Extensions.Logging;

namespace TestUIApp.Views;

public partial class MainWindow : Window
{
    private FFMPEGChannel? _testChannel;
    private IMediaChannel? _tstAudioChannel;
    private ComponentManager? _componentManager;
    private FFMPEGBackend? _ffBackend;
    private BassBackend? _bassBackend;
    private LocalDataService? _localBackend;
    
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override async void OnInitialized()
    {
        base.OnInitialized();
        var logFactory = LoggerFactory.Create(opt => opt.AddConsole());
        _componentManager = new ComponentManager(logFactory);
        _localBackend = _componentManager.CreateComponent<LocalDataService>("LocalBackend", 0);
        if (_localBackend == null)
            return;
        _ffBackend= _componentManager.CreateComponent<FFMPEGBackend>("FFMPEGBackend", 0);
        if (_ffBackend == null)
            return;
        
        _bassBackend = _componentManager.CreateComponent<BassBackend>("BASSBackend", 0);
        if (_bassBackend == null)
            return;

        var could = await _ffBackend.InitializeAsync();
        var could2 = await _bassBackend.InitializeAsync();
        if (!could || !could2)
            throw new Exception("Failed to initialize backends");
    }

    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_testChannel != null)
        {
            await _testChannel.StopAsync();
            await _testChannel.DisposeAsync();
            _testChannel = null;
        }
        if (_tstAudioChannel != null)
        {
            await _tstAudioChannel.StopAsync();
            await _tstAudioChannel.DisposeAsync();
            _tstAudioChannel = null;
        }
        
        var path = PathTextBox.Text;
        if (string.IsNullOrEmpty(path))
            return;
        if (path.StartsWith("\"") && path.EndsWith("\""))
            path = path[1..^1];
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return;
        
        var mediaItem = await _localBackend!.GetMediaItemAsync(path);
        if (mediaItem == null)
            return;
        await mediaItem.CacheAsync();
        
        _testChannel = new FFMPEGChannel(_ffBackend!, mediaItem);
        var couldPrepare = await _testChannel.PrepareAsync();
        if (!couldPrepare)
            return;
        
        var audioChannel = await _bassBackend!.CreateChannelAsync(mediaItem);
        if (audioChannel == null)
            return;
        
        _tstAudioChannel = audioChannel;
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            VideoPlayer.Content = new FFMPEGVideoControl(_testChannel);
            await _testChannel.PlayAsync();
            await _tstAudioChannel.PlayAsync();
        });
    }
}