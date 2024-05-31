using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using LibVLCSharp.Shared;
using Manager.MediaBackends.LibVLCPlayer;
using Manager.Shared;
using Microsoft.Extensions.Logging;

namespace Manager.MediaBackends.UI.LibVLCPlayer.Avalonia;

public class VLCVideoControl : NativeControlHost, IVLCVideoControl
{
    private readonly ILogger<VLCVideoControl>? _logger;
    private IPlatformHandle? _platformHandle;
    private readonly LibVLCChannel _channel;
    public MediaPlayer MediaPlayer { get; }

    public bool IsPlayReady { get; private set; }

    private string _surfaceName = "VLCVideoControl";

    public static readonly DirectProperty<VLCVideoControl, string> SurfaceNameProperty =
        AvaloniaProperty.RegisterDirect<VLCVideoControl, string>(
            nameof(SurfaceName), o => o.SurfaceName, (o, v) => o.SurfaceName = v);

    public string SurfaceName
    {
        get => _surfaceName;
        set => SetAndRaise(SurfaceNameProperty, ref _surfaceName, value);
    }

    public VLCVideoControl(ComponentManager componentManager, string? name, LibVLCChannel channel)
    {
        this._logger = componentManager.CreateLogger<VLCVideoControl>();
        this._channel = channel;
        this.MediaPlayer = channel.Player;
        this.SurfaceName = name ?? "VLCVideoControl";
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        var handle = parent.Handle;
        this._platformHandle = parent;
        this._channel.Player.Hwnd = handle;
        return base.CreateNativeControlCore(parent);
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        this._platformHandle = null;
        this._channel.Player.Hwnd = IntPtr.Zero;
        base.DestroyNativeControlCore(control);
    }

    public IntPtr? GetNativeReference()
    {
        return this._platformHandle?.Handle;
    }
}