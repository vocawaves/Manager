using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using LibVLCSharp.Shared;
using Manager.MediaBackends.LibVLCPlayer;
using Manager.Shared;
using Manager.Shared.Interfaces.General;
using Microsoft.Extensions.Logging;

namespace Manager.MediaBackends.UI.LibVLCPlayer.Avalonia;

public class VLCVideoControl : NativeControlHost, IManagerComponent, IVLCVideoControl
{
    #region IManagerComponent

    public ComponentManager ComponentManager { get; }
    
    string IManagerComponent.Name => ManComName;
    public string ManComName { get; private set; }
    
    ulong IManagerComponent.Parent => ManComParent;
    public ulong ManComParent { get; private set; }

    #endregion

    private readonly ILogger<VLCVideoControl>? _logger;
    private IPlatformHandle? _platformHandle;

    public static readonly StyledProperty<MediaPlayer?> CurrentPlayerProperty =
        AvaloniaProperty.Register<VLCVideoControl, MediaPlayer?>(
            nameof(CurrentPlayer));

    public MediaPlayer? CurrentPlayer
    {
        get => GetValue(CurrentPlayerProperty);
        set => SetValue(CurrentPlayerProperty, value);
    }

    private string _surfaceName = "VLCVideoControl";

    public static readonly DirectProperty<VLCVideoControl, string> SurfaceNameProperty =
        AvaloniaProperty.RegisterDirect<VLCVideoControl, string>(
            nameof(SurfaceName), o => o.SurfaceName, (o, v) => o.SurfaceName = v);

    public string SurfaceName
    {
        get => _surfaceName;
        set => SetAndRaise(SurfaceNameProperty, ref _surfaceName, value);
    }

    public VLCVideoControl(ComponentManager componentManager, string name, ulong parent)
    {
        this.ComponentManager = componentManager;
        this.ManComName = name;
        this.ManComParent = parent;
        this._logger = componentManager.CreateLogger<VLCVideoControl>();
    }
    
    public ValueTask<bool> SetPlayerToControlAsync(object playerObj)
    {
        if (playerObj is not MediaPlayer player) 
            return ValueTask.FromResult(false);
        if (Equals(this.CurrentPlayer, player)) 
            return ValueTask.FromResult(true);
        this.CurrentPlayer = player;
        if (this._platformHandle != null) 
            this.CreateNativeControlCore(this._platformHandle);
        return ValueTask.FromResult(true);
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        var handle = parent.Handle;
        this._platformHandle = parent;
        if (this.CurrentPlayer != null)
            this.CurrentPlayer.Hwnd = handle;
        return base.CreateNativeControlCore(parent);
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        this._platformHandle = null;
        if (this.CurrentPlayer != null)
            this.CurrentPlayer.Hwnd = IntPtr.Zero;
        base.DestroyNativeControlCore(control);
    }
}