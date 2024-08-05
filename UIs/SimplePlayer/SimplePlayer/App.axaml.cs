﻿using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Manager.Shared;
using Manager.SimplePlayer;
using Microsoft.Extensions.Logging;
using Serilog;
using SimplePlayer.ViewModels;
using SimplePlayer.Views;

namespace SimplePlayer;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        var logConf = new LoggerConfiguration().MinimumLevel.Debug()
#if DEBUG
            .WriteTo.Console()
#endif
            .WriteTo.File(Path.Combine(Directory.GetCurrentDirectory(), "SimplePlayer.log"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog(logConf));
        var logger = loggerFactory.CreateLogger<App>();
        var componentManager = new ComponentManager(loggerFactory);
        var mediaPlayer = componentManager.CreateComponent<MediaPlayer>("MediaPlayer", 0);
        if (mediaPlayer == null)
        {
            logger.LogError("Failed to create media player.");
            return;
        }

        var couldInitialize = await mediaPlayer.InitializeAsync().ConfigureAwait(true);
        if (!couldInitialize)
        {
            logger.LogError("Failed to initialize media player.");
            return;
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(componentManager, mediaPlayer)
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel(componentManager, mediaPlayer)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}