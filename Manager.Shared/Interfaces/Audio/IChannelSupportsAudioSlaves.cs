using Manager.Shared.Entities;
using Manager.Shared.Events.Audio;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.Audio;

/// <summary>
/// Some backend services support audio slaves.
/// For example, libVLC can have audio and subtitle slaves.
/// </summary>
public interface IChannelSupportsAudioSlaves
{
    /// <summary>
    /// Fired when an audio slave is added to this channel.
    /// </summary>
    public event AsyncEventHandler<ChannelAudioSlaveAddedEventArgs>? AudioSlaveAdded;
    /// <summary>
    /// Fired when an audio slave is removed from this channel.
    /// </summary>
    public event AsyncEventHandler<ChannelAudioSlaveRemovedEventArgs>? AudioSlaveRemoved;
    
    /// <summary>
    /// This channel's audio slaves. Should only be managed by the channel.
    /// </summary>
    public List<AudioItem> AudioSlaves { get; } //TODO: make this something that cant be modified from outside
    
    /// <summary>
    /// Adds an audio slave to this channel.
    /// Also fires the <see cref="AudioSlaveAdded"/> event.
    /// </summary>
    public ValueTask<bool> AddAudioSlaveAsync(AudioItem audioItem);
    /// <summary>
    /// Removes an audio slave from this channel.
    /// Also fires the <see cref="AudioSlaveRemoved"/> event.
    /// </summary>
    public ValueTask<bool> RemoveAudioSlaveAsync(AudioItem audioItem);
    
}