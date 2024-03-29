using Manager.Shared.Entities;
using Manager.Shared.Events.Audio;
using Manager.Shared.Helpers;
using Manager.Shared.Interfaces.General;

namespace Manager.Shared.Interfaces.Audio;

/// <summary>
/// An Audio Channel is a channel that can play audio.
/// To the regular MediaChannel, this adds the ability to control the volume and audio device.
/// </summary>
public interface IAudioChannel : IMediaChannel
{
    #region Events

    //VolumeChanged
    /// <summary>
    /// Fired when the volume of this specific channel changes.
    /// This is detached from the global volume.
    /// </summary>
    public event AsyncEventHandler<ChannelVolumeChangedEventArgs>? ChannelVolumeChanged; 
    //DeviceChanged
    /// <summary>
    /// Fired when the device of this specific channel changes.
    /// This is detached from the global device.
    /// </summary>
    public event AsyncEventHandler<ChannelDeviceChangedEventArgs>? ChannelDeviceChanged;

    #endregion
    
    /// <summary>
    /// Get the current volume of this channel.
    /// </summary>
    public ValueTask<float?> GetVolumeAsync();
    /// <summary>
    /// Sets the volume of this channel.
    /// Also fires the <see cref="ChannelVolumeChanged"/> event.
    /// </summary>
    public ValueTask<bool> SetVolumeAsync(float volume);
    
    /// <summary>
    /// Gets the current audio device this channel is using.
    /// </summary>
    public ValueTask<AudioDevice?> GetDeviceAsync();
    /// <summary>
    /// Sets the audio device this channel should use.
    /// Also fires the <see cref="ChannelDeviceChanged"/> event.
    /// </summary>
    public ValueTask<bool> SetDeviceAsync(AudioDevice device);
}