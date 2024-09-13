using System.IO;
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
        if (!File.Exists(path))
            return;

        Decoder = new VideoDecoder();

        if (!Decoder.InitializeFromFile(path))
            return;

        var control = new VideoControl(Decoder);
        var control1 = new VideoControl(Decoder, control.VideoBitmap);
        var control2 = new VideoControl(Decoder, control.VideoBitmap);
        var control3 = new VideoControl(Decoder, control.VideoBitmap);
        Video0.Content = control;
        Video1.Content = control1;
        Video2.Content = control2;
        Video3.Content = control3;
        //set up audio
        Reader = new MediaFoundationReader(path);
        WasapiOut = new WasapiOut();
        WasapiOut.Init(Reader);
        WasapiOut.Play();
        Decoder.Start();
    }
}