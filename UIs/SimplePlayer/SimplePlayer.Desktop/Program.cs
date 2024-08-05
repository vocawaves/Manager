using System;
using System.IO;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;

namespace SimplePlayer.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Death.log"), e.ToString());
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<SimplePlayer.App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}