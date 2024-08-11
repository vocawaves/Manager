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

    private byte_ptrArray8 _targetData = new byte_ptrArray8();
    private int_array8 _targetLineSize = new int_array8();

    private unsafe void OnFramePushed()
    {
        if (_channel.Decoder == null)
            return;
        var frame = _channel.Decoder.Frames.Dequeue();
        _imageBitmap ??= new WriteableBitmap(new PixelSize(_channel.Decoder.FrameWidth, _channel.Decoder.FrameHeight), new Vector(96, 96),
            PixelFormat.Bgra8888);

        var bitmapLock = _imageBitmap.Lock();
        var data = (byte*)bitmapLock.Address;
        var lineSize = bitmapLock.RowBytes;

        try
        {
            _channel.Decoder.FrameConvert(&frame, ref _targetData, ref _targetLineSize);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return;
        }
        for (var i = 0; i < frame.height; i++)
        {
            Unsafe.CopyBlock(data + i * lineSize, _targetData[0] + i * _targetLineSize[0], (uint)_targetLineSize[0]);
        }

        bitmapLock.Dispose();
        Dispatcher.UIThread.InvokeAsync(() => { _imageContent.Source = _imageBitmap; });
    }
}