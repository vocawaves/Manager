using Avalonia.Controls;
using Avalonia.Platform;
using Microsoft.Extensions.Logging;

namespace Manager.LibVLCPlayer.Avalonia;

public class VLCVideoControl : NativeControlHost
{
    private readonly ILogger<VLCVideoControl>? _logger;
    private IPlatformHandle? _platformHandle;

    public VLCVideoControl(string? name = null, ILogger<VLCVideoControl>? logger = null)
    {
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        var handle = parent.Handle;
        //set HWND
        this._platformHandle = parent;
        return base.CreateNativeControlCore(parent);
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        //destroy HWND
        this._platformHandle = null;
        base.DestroyNativeControlCore(control);
    }
}