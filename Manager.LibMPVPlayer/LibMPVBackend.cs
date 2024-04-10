using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Events.General;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.Video;
using Nickvision.MPVSharp;

namespace Manager.LibMPVPlayer;

public class LibMPVBackend : IVideoBackendService
{
    public event AsyncEventHandler<InitSuccessEventArgs>? InitSuccess;
    public event AsyncEventHandler<InitFailedEventArgs>? InitFailed;
    public bool Initialized { get; } = true;
    public string Name { get; }
    public ulong Parent { get; }
    
    public LibMPVBackend(string name, ulong parent)
    {
        this.Name = name;
        this.Parent = parent;
    }
    
    public ValueTask<bool> InitializeAsync(params string[] options)
    {
        this.InitSuccess?.InvokeAndForget(this, new InitSuccessEventArgs("MPV initialized"));
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> IsMediaItemSupportedAsync(MediaItem mediaItem)
    {
        throw new NotImplementedException();
    }

    public async ValueTask<IVideoChannel?> CreateChannelAsync(VideoItem item)
    {
        if (item.CacheState != CacheState.Cached)
            return null;

        var path = await item.GetCachePathAsync();
        if (path == null)
            return null;
        
        var mpvClient = new Client();
        mpvClient.Initialize();
        mpvClient.LoadFile(path); //Maybe??
        mpvClient.SetProperty("pause", "yes");
        return new LibMPVChannel(this, mpvClient, item, path);
    }
}