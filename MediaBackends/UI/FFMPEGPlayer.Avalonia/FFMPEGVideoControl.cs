using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace FFMPEGPlayer.Avalonia;

public class FFMPEGVideoControl : Image
{
    private readonly FFMPEGChannel _channel;
    private readonly WriteableBitmap _videoSource;

    public FFMPEGVideoControl(FFMPEGChannel channel) : base()
    {
        _channel = channel;
        if (_channel.Decoder == null)
            throw new ArgumentNullException(nameof(channel.Decoder));
        _videoSource = new WriteableBitmap(
            new PixelSize(channel.Decoder!.FrameWidth, channel.Decoder!.FrameHeight), new Vector(96, 96),
            PixelFormat.Bgra8888);
        Source = _videoSource;
        _channel.Decoder.OnFrameDecoded = OnFramePushed;
    }

    private unsafe void OnFramePushed()
    {
        var frame = _channel.Decoder!.MainFrame;
        using (var bitmapLock = this._videoSource.Lock())
        {
            for (var i = 0; i < frame->height; i++)
            {
                Unsafe.CopyBlock((bitmapLock.Address + bitmapLock.RowBytes * i).ToPointer(),
                    (_channel.Decoder._targetData[0] + i * _channel.Decoder._targetLineSize[0]),
                    (uint)_channel.Decoder._targetLineSize[0]);
            }
        }
        Dispatcher.UIThread.InvokeAsync(InvalidateVisual);
    }
}