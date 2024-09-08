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
        Video.Content = control;
        
        //set up audio
        Reader = new MediaFoundationReader(path);
        WasapiOut = new WasapiOut();
        WasapiOut.Init(Reader);
        Decoder.Start();
        WasapiOut.Play();
    }
}