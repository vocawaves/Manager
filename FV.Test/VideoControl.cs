using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;

namespace FV.Test;

public class VideoControl : Image
{
    private readonly bool _isDecoderOwner = true;
    private readonly VideoDecoder _decoder;
    public WriteableBitmap VideoBitmap { get; }

    public VideoControl(VideoDecoder decoder)
    {
        _decoder = decoder;
        VideoBitmap = new WriteableBitmap(
            new PixelSize(decoder.FrameWidth, decoder.FrameHeight), new Vector(96, 96),
            PixelFormat.Bgra8888, AlphaFormat.Premul);
        Source = VideoBitmap;
        _decoder.OnFrameDecoded += OnFramePushed;
    }

    public VideoControl(VideoDecoder decoder, WriteableBitmap bitmap)
    {
        _isDecoderOwner = false;
        _decoder = decoder;
        VideoBitmap = bitmap;
        Source = VideoBitmap;
        _decoder.OnFrameDecoded += OnFramePushed;
    }

    private void OnFramePushed()
    {
        if (_isDecoderOwner)
            using (var bitmapLock = VideoBitmap.Lock())
                _decoder.CopyToBitmap(bitmapLock.Address, bitmapLock.RowBytes);
        Dispatcher.UIThread.InvokeAsync(InvalidateVisual);
    }
}