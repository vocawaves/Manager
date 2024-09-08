using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace FV.Test;

public class VideoControl : Image
{
    private readonly VideoDecoder _decoder;
    private readonly WriteableBitmap _videoBitmap;

    public VideoControl(VideoDecoder decoder)
    {
        _decoder = decoder;
        _videoBitmap = new WriteableBitmap(
            new PixelSize(decoder.FrameWidth, decoder.FrameHeight), new Vector(96, 96),
            PixelFormat.Bgra8888, AlphaFormat.Premul);
        Source = _videoBitmap;
        _decoder.OnFrameDecoded = OnFramePushed;
    }

    private void OnFramePushed()
    {
        using (var bitmapLock = _videoBitmap.Lock()) 
            _decoder.CopyToBitmap(bitmapLock.Address, bitmapLock.RowBytes);
        Dispatcher.UIThread.InvokeAsync(InvalidateVisual);
    }
}