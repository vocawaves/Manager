using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using LibVLCSharp.Shared;
using Manager.MediaBackends.LibVLCPlayer;
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
            "SurfaceName", o => o.SurfaceName, (o, v) => o.SurfaceName = v);

    public string SurfaceName
    {
        get => _surfaceName;
        set => SetAndRaise(SurfaceNameProperty, ref _surfaceName, value);
    }

    public VLCVideoControl(string? name, LibVLCChannel channel, ILogger<VLCVideoControl>? logger = null)
    {
        this._logger = logger;
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

    public async ValueTask<bool> PrepareForPlayAsync(int syncTime = 150)
    {
        if (this._platformHandle == null)
        {
            this._logger?.LogError("Platform handle is null, cannot prepare for play.");
            return false;
        }

        //just in case disable audio track
        var oldAudioTrack = this.MediaPlayer.AudioTrack;
        this.MediaPlayer.SetAudioTrack(-1);
        var oldHeight = this.Height;
        var oldWidth = this.Width;
        this.Height = 1; //Maybe even 0
        this.Width = 1;
        var playTcs = new TaskCompletionSource<bool>();
        var vOutTcs = new TaskCompletionSource<bool>();

        void OnPlaying(object? sender, EventArgs e)
        {
            playTcs.SetResult(true);
            this.MediaPlayer.Playing -= OnPlaying;
        }

        void OnVOut(object? sender, EventArgs e)
        {
            vOutTcs.SetResult(true);
            this.MediaPlayer.Vout -= OnVOut;
        }

        this.MediaPlayer.Playing += OnPlaying;
        this.MediaPlayer.Vout += OnVOut;
        await await Task.Factory.StartNew(async () =>
        {
            this.MediaPlayer.Play();
            await Task.WhenAll(playTcs.Task, vOutTcs.Task);
        });
        await Task.Delay(syncTime);
        var pauseTcs = new TaskCompletionSource<bool>();

        void OnPaused(object? sender, EventArgs e)
        {
            pauseTcs.SetResult(true);
            this.MediaPlayer.Paused -= OnPaused;
        }

        this.MediaPlayer.Paused += OnPaused;
        await await Task.Factory.StartNew(async () =>
        {
            this.MediaPlayer.Pause();
            await pauseTcs.Task;
        });
        await Task.Delay(syncTime);
        await Task.Factory.StartNew(() => this.MediaPlayer.Time = 0);
        await Task.Delay(syncTime);
        this.MediaPlayer.SetAudioTrack(oldAudioTrack);
        this.Height = oldHeight;
        this.Width = oldWidth;
        this.IsPlayReady = true;
        return true;
    }
}