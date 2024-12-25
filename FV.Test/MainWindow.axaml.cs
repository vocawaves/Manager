using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using NAudio.Wave;

namespace FV.Test;

public partial class MainWindow : Window
{
    public VideoDecoder? Decoder { get; set; }
    public MediaFoundationReader? Reader { get; set; }
    public WasapiOut? WasapiOut { get; set; }

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Decoder != null || Reader != null || WasapiOut != null)
        {
            Decoder?.Stop();
            Decoder?.Dispose();
            Decoder = null;
            Reader?.Dispose();
            Reader = null;
            WasapiOut?.Dispose();
            WasapiOut = null;
        }

        var path = PathBox.Text;
        if (string.IsNullOrWhiteSpace(path))
            return;
        if (path.StartsWith("\"") || path.StartsWith("\""))
            path = path[1..^1];
        //if (!File.Exists(path))
        //    return;

        Decoder = new VideoDecoder();

        if (!Decoder.InitializeFromFile(path))
            return;

        var control = new AniVideoControl(Decoder);
        var control1 = new AniVideoControl(Decoder, control.WBitmap);
        var control2 = new AniVideoControl(Decoder, control.WBitmap);
        Video0.Content = control;
        var window = new Window
        {
            Content = control1
        };
        window.Show();
        window.Position = Screens.All[1].Bounds.Center;
        window.WindowState = WindowState.FullScreen;
        //var window1 = new Window
        //{
        //    Content = control2
        //};
        //window1.Show();
        //window1.Position = Screens.All[2].Bounds.Center;
        //window1.WindowState = WindowState.FullScreen;
        Reader = new MediaFoundationReader(path);
        WasapiOut = new WasapiOut();
        WasapiOut.Init(Reader);
        WasapiOut.Play();
        Decoder.Start();
    }
}