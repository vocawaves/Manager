using LibVLCSharp.Shared;
using Manager.Shared;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces;

namespace Manager.Services.VlcVideo;

public class LibVlcChannel : IVideoChannel, IAudioChannel, ISubtitleChannel,
    IChannelSupportsVideoStreamSelection, IChannelSupportsAudioStreamSelection, IChannelSupportsSubtitleStreamSelection,
    IChannelSupportsAudioSlaves, IChannelSupportsSubtitleSlaves
{
    public IVideoBackendService AssociatedVideoBackend { get; }
    public IAudioBackendService AssociatedAudioBackend { get; }

    public ISubtitleBackendService AssociatedSubtitleBackend { get; }
    public PlayItem PlayItem { get; }
    public Media LibVlcMedia { get; }
    
    public float ChannelVolume { get; internal set; } = 1f;
    
    public int VideoStreamIndex { get; internal set; }
    public int AudioStreamIndex { get; internal set; }
    public int SubtitleStreamIndex { get; internal set; }
    
    
    public List<PlayItem> AudioSlaves { get; } = new();
    private Dictionary<PlayItem, MediaSlave> _internalAudioSlaves = new();
    public List<PlayItem> SubtitleSlaves { get; } = new();
    private Dictionary<PlayItem, MediaSlave> _internalSubtitleSlaves = new();

    public LibVlcChannel(ManagerComponent component, PlayItem playItem, Media media)
    {
        if (component is not (IVideoBackendService ivs and IAudioBackendService ias and ISubtitleBackendService iss))
            throw new ArgumentException("Component must implement IVideoBackendService, IAudioBackendService and ISubtitleBackendService");
        
        this.AssociatedVideoBackend = ivs;
        this.AssociatedAudioBackend = ias;
        this.AssociatedSubtitleBackend = iss;
        this.PlayItem = playItem;
        this.LibVlcMedia = media;
        //Set default stream indexes
        this.VideoStreamIndex = media.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Video).Id;
        this.AudioStreamIndex = media.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Audio).Id;
        this.SubtitleStreamIndex = media.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Text).Id;
    }
    
    public ValueTask<bool> PlayAsync()
    {
        return this.AssociatedVideoBackend.PlayChannelAsync(this);
    }

    public ValueTask<bool> PauseAsync()
    {
        return this.AssociatedVideoBackend.PauseChannelAsync(this);
    }

    public ValueTask<bool> ResumeAsync()
    {
        return this.AssociatedVideoBackend.ResumeChannelAsync(this);
    }

    public ValueTask<bool> StopAsync()
    {
        return this.AssociatedVideoBackend.StopChannelAsync(this);
    }

    public ValueTask<ChannelState?> GetStateAsync()
    {
        return this.AssociatedVideoBackend.GetChannelStateAsync(this);
    }

    public ValueTask<bool> SetStateAsync(ChannelState state)
    {
        return this.AssociatedVideoBackend.SetChannelStateAsync(this, state);
    }
    
    public ValueTask<float?> GetVolumeAsync()
    {
        if (this.AssociatedVideoBackend is LibVlcVideoBackendService lbs && !Equals(lbs.MediaPlayer?.Media, this.LibVlcMedia))
            return ValueTask.FromResult((float?)this.ChannelVolume); // Volume will be set when the channel is played
        return this.AssociatedAudioBackend.GetChannelVolumeAsync(this);
    }

    public ValueTask<bool> SetVolumeAsync(float volume)
    {
        this.ChannelVolume = volume;
        if (this.AssociatedVideoBackend is LibVlcVideoBackendService lbs && !Equals(lbs.MediaPlayer?.Media, this.LibVlcMedia))
            return ValueTask.FromResult(true); // Volume will be set when the channel is played
        return this.AssociatedAudioBackend.SetChannelVolumeAsync(this, volume);
    }

    public ValueTask<AudioDevice?> GetDeviceAsync()
    {
        return this.AssociatedAudioBackend.GetChannelDeviceAsync(this);
    }

    public ValueTask<bool> SetDeviceAsync(AudioDevice device)
    {
        return this.AssociatedAudioBackend.SetChannelDeviceAsync(this, device);
    }

    public ValueTask<TimeSpan?> GetPositionAsync()
    {
        return this.AssociatedVideoBackend.GetChannelPositionAsync(this);
    }

    public ValueTask<bool> SetPositionAsync(double positionMs)
    {
        return this.AssociatedVideoBackend.SetChannelPositionAsync(this, positionMs);
    }

    public ValueTask<bool> SetPositionAsync(TimeSpan position)
    {
        return this.AssociatedVideoBackend.SetChannelPositionAsync(this, position);
    }

    public ValueTask<TimeSpan?> GetLengthAsync()
    {
        return this.AssociatedVideoBackend.GetChannelLengthAsync(this);
    }

    public ValueTask<SelectableMediaStream[]?> GetSelectableVideoStreamsAsync()
    {
        var asSupported = (IBackendSupportsVideoStreamSelection)this.AssociatedVideoBackend;
        return asSupported.GetSelectableVideoStreamsAsync(this);
    }

    public ValueTask<bool> SetSelectedVideoStreamAsync(SelectableMediaStream? stream)
    {
        var asSupported = (IBackendSupportsVideoStreamSelection)this.AssociatedVideoBackend;
        return asSupported.SetSelectedVideoStreamAsync(this, stream);
    }

    public ValueTask<SelectableMediaStream[]?> GetSelectableAudioStreamsAsync()
    {
        var asSupported = (IBackendSupportsAudioStreamSelection)this.AssociatedAudioBackend;
        return asSupported.GetSelectableAudioStreamsAsync(this);
    }

    public ValueTask<bool> SetSelectedAudioStreamAsync(SelectableMediaStream? stream)
    {
        var asSupported = (IBackendSupportsAudioStreamSelection)this.AssociatedAudioBackend;
        return asSupported.SetSelectedAudioStreamAsync(this, stream);
    }

    public ValueTask<SelectableMediaStream[]?> GetSelectableSubtitleStreamsAsync()
    {
        var asSupported = (IBackendSupportsSubtitleStreamSelection)this.AssociatedSubtitleBackend;
        return asSupported.GetSelectableSubtitleStreamsAsync(this);
    }

    public ValueTask<bool> SetSelectedSubtitleStreamAsync(SelectableMediaStream? stream)
    {
        var asSupported = (IBackendSupportsSubtitleStreamSelection)this.AssociatedSubtitleBackend;
        return asSupported.SetSelectedSubtitleStreamAsync(this, stream);
    }
    
    public async ValueTask<bool> AddAudioSlaveAsync(PlayItem playItem)
    {
        if (this._internalAudioSlaves.ContainsKey(playItem))
            return true; // Already added
        
        if (playItem.CacheState == CacheState.NotCached)
            return false;

        if (playItem.CacheState == CacheState.Memory)
        {
            //Cannot add a slave from memory :(
            return false;
        }

        var cachePath = await playItem.CacheStrategy.GetCachedPathAsync(playItem);
        if (cachePath == null)
            return false;
        var couldAddSlave = this.LibVlcMedia.AddSlave(MediaSlaveType.Audio, 0, cachePath);
        if (!couldAddSlave)
            return false;
        var slave = this.LibVlcMedia.Slaves.Last();
        this._internalAudioSlaves.Add(playItem, slave);
        this.AudioSlaves.Add(playItem);
        return true;
    }

    public async ValueTask<bool> RemoveAudioSlaveAsync(PlayItem playItem)
    {
        if (!this._internalAudioSlaves.ContainsKey(playItem))
            return true; // Already removed
        
        //There is only a clear all slaves method, so we need to remove all slaves and re-add the ones we want to keep
        this.LibVlcMedia.ClearSlaves();
        this._internalAudioSlaves.Remove(playItem);
        this.AudioSlaves.Remove(playItem);
        foreach (var (item, index) in this._internalAudioSlaves)
        {
            var cachePath = await item.CacheStrategy.GetCachedPathAsync(item);
            if (cachePath == null)
                continue; //failed to get cache path, skip
            var couldAddSlave = this.LibVlcMedia.AddSlave(index.Type, 0, cachePath);
            if (!couldAddSlave)
                continue; //failed to add slave, skip
            var slave = this.LibVlcMedia.Slaves.Last();
            this._internalAudioSlaves[item] = slave;
        }
        return true;
    }

    public async ValueTask<bool> AddSubtitleSlaveAsync(PlayItem playItem)
    {
        if (this._internalSubtitleSlaves.ContainsKey(playItem))
            return true; // Already added
        
        if (playItem.CacheState == CacheState.NotCached)
            return false;
        
        if (playItem.CacheState == CacheState.Memory)
            return false; //Cannot add a slave from memory :(
        
        var cachePath = await playItem.CacheStrategy.GetCachedPathAsync(playItem);
        if (cachePath == null)
            return false;
        
        var couldAddSlave = this.LibVlcMedia.AddSlave(MediaSlaveType.Subtitle, 0, cachePath);
        if (!couldAddSlave)
            return false;
        
        var slave = this.LibVlcMedia.Slaves.Last();
        this._internalSubtitleSlaves.Add(playItem, slave);
        this.SubtitleSlaves.Add(playItem);
        return true;
    }

    public async ValueTask<bool> RemoveSubtitleSlaveAsync(PlayItem playItem)
    {
        if (!this._internalSubtitleSlaves.ContainsKey(playItem))
            return true; // Already removed
        
        //There is only a clear all slaves method, so we need to remove all slaves and re-add the ones we want to keep
        this.LibVlcMedia.ClearSlaves();
        this._internalSubtitleSlaves.Remove(playItem);
        this.SubtitleSlaves.Remove(playItem);
        
        foreach (var (item, index) in this._internalSubtitleSlaves)
        {
            var cachePath = await item.CacheStrategy.GetCachedPathAsync(item);
            if (cachePath == null)
                continue; //failed to get cache path, skip
            var couldAddSlave = this.LibVlcMedia.AddSlave(index.Type, 0, cachePath);
            if (!couldAddSlave)
                continue; //failed to add slave, skip
            this._internalSubtitleSlaves[item] = index;
        }
        return true;
    }

    public ValueTask<bool> DestroyAsync()
    {
        return this.AssociatedVideoBackend.DestroyChannelAsync(this);
    }
    
    public async ValueTask DisposeAsync()
    {
        await this.DestroyAsync();
    }
}