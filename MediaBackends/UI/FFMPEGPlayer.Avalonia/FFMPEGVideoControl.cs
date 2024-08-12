using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using FFmpeg.AutoGen;

namespace FFMPEGPlayer.Avalonia;

public class FFMPEGVideoControl : UserControl
{
    private readonly Image _imageContent;
    private WriteableBitmap? _imageBitmap;
    private readonly FFMPEGChannel _channel;

    public FFMPEGVideoControl(FFMPEGChannel channel)
    {
        _channel = channel;
        _imageContent = new Image()
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            StretchDirection = StretchDirection.Both
        };
        Content = _imageContent;
        if (_channel.Decoder == null)
            return;
        _channel.Decoder.OnFrameDecoded = OnFramePushed;
    }

    private unsafe void OnFramePushed()
    {
        if (_channel.Decoder == null)
            return;
        var frame = _channel.Decoder.MainFrame;
        if (_imageBitmap == null)
        {
            _imageBitmap = new WriteableBitmap(new PixelSize(_channel.Decoder.FrameWidth, _channel.Decoder.FrameHeight), new Vector(96, 96),
                PixelFormat.Bgra8888);
            Dispatcher.UIThread.InvokeAsync(() => _imageContent.Source = _imageBitmap);
        }

        var bitmapLock = _imageBitmap.Lock();
        for (var i = 0; i < frame->height; i++)
        {
            Unsafe.CopyBlock((bitmapLock.Address + bitmapLock.RowBytes * i).ToPointer(),
                (_channel.Decoder._targetData[0] + i * _channel.Decoder._targetLineSize[0]), (uint)_channel.Decoder._targetLineSize[0]);
        }

        bitmapLock.Dispose();
        Dispatcher.UIThread.InvokeAsync(() => { _imageContent.InvalidateVisual(); });
    }
}