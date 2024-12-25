using Avalonia;
using System;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace FV.Test;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .With(new SkiaOptions()
            {
                MaxGpuResourceSizeBytes = 1920 * 1080 * 4 * 100, // ~800mb 100x 1920 x 1080 textures.
            })
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}