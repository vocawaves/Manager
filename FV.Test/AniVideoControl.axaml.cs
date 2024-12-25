using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace FV.Test;

public partial class AniVideoControl : UserControl
{
    private TopLevel? _topLevel;
    public WriteableBitmap WBitmap { get; }
    public VideoDecoder Decoder { get; }

    public AniVideoControl()
    {
        InitializeComponent();
    }
    
    public AniVideoControl(VideoDecoder decoder)
    {
        InitializeComponent();
        Decoder = decoder;
        WBitmap = new WriteableBitmap(new PixelSize(decoder.FrameWidth, decoder.FrameHeight), new Vector(96, 96),
            PixelFormat.Bgra8888, AlphaFormat.Premul);
        decoder.OnFrameDecoded += OnFramePushed;
        VidImage.Source = WBitmap;
        RenderOptions.SetBitmapInterpolationMode(VidImage, BitmapInterpolationMode.HighQuality);
    }
    
    public AniVideoControl(VideoDecoder decoder, WriteableBitmap bitmap)
    {
        InitializeComponent();
        Decoder = decoder;
        WBitmap = bitmap;
        decoder.OnFrameDecoded += OnFramePushed;
        VidImage.Source = WBitmap;
    }

    private bool _started = false;
    private bool _visible = false;

    private void OnFramePushed()
    {
        if (_started)
            return;
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.RequestAnimationFrame(OnFrame);
    }

    private void OnFrame(TimeSpan obj)
    {
        if (Decoder.MainFrame == null || _started == false || _visible == false)
            return;
        using var wLock = WBitmap.Lock();
        Decoder.CopyToBitmap(wLock.Address, wLock.RowBytes);
        VidImage.InvalidateVisual();
        _topLevel?.RequestAnimationFrame(OnFrame);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _topLevel = null;
        _started = false;
        _visible = false;
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _visible = true;
        if (_started)
            return;
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.RequestAnimationFrame(OnFrame);
        _started = true;
    }
}