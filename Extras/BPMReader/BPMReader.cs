using ManagedBass;
using ManagedBass.Fx;
using Manager.MediaBackends.BassPlayer;
using Manager.Shared;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.General;
using Manager.Shared.Extensions;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.General;
using Microsoft.Extensions.Logging;

namespace BPMReader;

public class BPMReader : IManagerComponent, INeedsInitialization
{
    public ComponentManager ComponentManager { get; }
    public string Name { get; }
    public ulong Parent { get; }

    public event AsyncEventHandler? InitSuccess;
    public event AsyncEventHandler<InitFailedEventArgs>? InitFailed;
    public bool Initialized { get; private set; }
    
    private readonly ILogger<BPMReader>? _logger;
    private BassBackend? _exisingBassBackend;

    public BPMReader(ComponentManager componentManager, string name, ulong parent)
    {
        ComponentManager = componentManager;
        Name = name;
        Parent = parent;
        
        //check if BassBackend already exists and is initialized (otherwise init is needed)
        var bassBackends = componentManager.Components.OfType<BassBackend>().ToList();
        if (!bassBackends.Any())
            return;
        
        _exisingBassBackend = bassBackends.First();
        if (!_exisingBassBackend.Initialized) 
            return;
        Initialized = true;
    }
    
    public ValueTask<bool> InitializeAsync(params string[] options)
    {
        if (Initialized)
        {
            this._logger?.LogInformation("Bass already initialized");
            return ValueTask.FromResult(true);
        }
        
        if (_exisingBassBackend?.Initialized == true)
        {
            Initialized = true;
            this._logger?.LogInformation("Bass already initialized");
            this.InitSuccess?.InvokeAndForget(this, EventArgs.Empty);
            return ValueTask.FromResult(true);
        }
        
        var bassInit = Bass.Init();
        if (!bassInit)
        {
            _logger?.LogError("Failed to initialize Bass: " + Bass.LastError);
            this.InitFailed?.InvokeAndForget(this, new InitFailedEventArgs("Failed to initialize Bass: " + Bass.LastError));
            return ValueTask.FromResult(false);
        }
        
        Initialized = true;
        this._logger?.LogInformation("BPMReader BASS initialized");
        this.InitSuccess?.InvokeAndForget(this, EventArgs.Empty);
        return ValueTask.FromResult(true);
    }
    
    public static async Task<double?> TryGetBPM(MediaItem mediaItem)
    {
        if (mediaItem.ItemType != ItemType.Audio && mediaItem.ItemType != ItemType.Video)
            return null;
        var path = await mediaItem.GetCachedPathAsync();
        if (path is null)
        {
            Console.WriteLine("Failed to get path");
            return null;
        }

        var stream = Bass.CreateStream(path, 0, 0, BassFlags.Default | BassFlags.Decode | BassFlags.Float);
        if (stream == 0)
        {
            Console.WriteLine("Failed to create stream: " + Bass.LastError);
            return null;
        }

        var length = Bass.ChannelBytes2Seconds(stream, Bass.ChannelGetLength(stream));
        var bpm = BassFx.BPMDecodeGet(stream, 0, length, 0, 0, null);
        if (bpm < 0)
        {
            Console.WriteLine("Failed to get BPM: " + Bass.LastError);
            Bass.StreamFree(stream);
            return null;
        }
        Bass.StreamFree(stream);
        return bpm;
    }
}