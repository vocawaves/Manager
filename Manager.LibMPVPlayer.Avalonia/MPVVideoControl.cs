using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Manager.Shared.Interfaces.Video;

namespace Manager.LibMPVPlayer.Avalonia;

public class MPVVideoControl : NativeControlHost, IExternalPlayerSurface
{
    private readonly LibMPVChannel _channel;
    private IPlatformHandle? _platformHandle;
    public string SurfaceName { get; }
    public IntPtr? GetNativeReference()
        => this._platformHandle?.Handle;

    public MPVVideoControl(LibMPVChannel channel,string surfaceName)
    {
        this._channel = channel;
        this.SurfaceName = surfaceName;
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        var handle = base.CreateNativeControlCore(parent);
        this._platformHandle = handle;
        this._channel.MPVClient.SetProperty("wid", handle.Handle);
        return handle;
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        base.DestroyNativeControlCore(control);
        this._channel.MPVClient.SetProperty("wid", 0);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var hwnd = this._platformHandle?.Handle ?? IntPtr.Zero;
        this._channel.MPVClient.SetProperty("wid", hwnd);
    }
    
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        this._channel.MPVClient.SetProperty("wid", 0);
    }

    public IVideoChannel? VideoChannel { get; set; }
    public static IExternalPlayerSurface? CreateSurface(IVideoChannel channel, string surfaceName)
    {
        return channel is LibMPVChannel libMPVChannel ? new MPVVideoControl(libMPVChannel, surfaceName) : null;
    }
}