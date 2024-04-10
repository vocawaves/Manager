using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using Manager.Shared.Interfaces.Video;

namespace Manager.LibVLCPlayer.Avalonia;

public class VLCVideoControl : NativeControlHost, IExternalPlayerSurface
{
    private readonly LibVLCChannel _channel;

    public static readonly StyledProperty<string> SurfaceNameProperty = AvaloniaProperty.Register<VLCVideoControl, string>(
        "SurfaceName");

    public string SurfaceName
    {
        get => GetValue(SurfaceNameProperty);
        private init => SetValue(SurfaceNameProperty, value);
    }

    public IntPtr? GetNativeReference() => this._platformHandle?.Handle;
    
    public IVideoChannel? VideoChannel { get; set; }
    
    private IPlatformHandle? _platformHandle;
    
    public VLCVideoControl(LibVLCChannel channel,  string surfaceName)
    {
        this._channel = channel;
        this.SurfaceName = surfaceName;
    }
    
    public static IExternalPlayerSurface CreateSurface(IVideoChannel channel, string surfaceName)
    {
        if (channel is not LibVLCChannel libVLCChannel)
            throw new ArgumentException("Channel is not a LibVLCChannel");
        
        return new VLCVideoControl(libVLCChannel, surfaceName);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this._channel.MediaPlayer.Hwnd = this._platformHandle?.Handle ?? IntPtr.Zero;
    }
    
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        this._channel.MediaPlayer.Hwnd = IntPtr.Zero;
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        this._platformHandle = base.CreateNativeControlCore(parent);
        return this._platformHandle;
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        base.DestroyNativeControlCore(control);
        this._platformHandle = null;
    }
}