using Manager.Shared.Entities;
using Manager.Shared.Events.Audio;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.Audio;

/// <summary>
/// Some backend services support audio stream selection in their channels.
/// For example, libVLC lets you select different audio, video, and subtitle streams.
/// </summary>
public interface IChannelSupportsAudioStreamSelection
{

    //SelectedAudioStreamChanged
    /// <summary>
    /// Fired when the selected audio stream of this channel changes.
    /// </summary>
    public event AsyncEventHandler<ChannelSelectedAudioStreamChangedEventArgs>? SelectedAudioStreamChanged;
    
    /// <summary>
    /// Gets all selectable audio streams of this channel.
    /// </summary>
    public ValueTask<SelectableMediaStream[]?> GetSelectableAudioStreamsAsync();
    /// <summary>
    /// Sets the selected audio stream of this channel.
    /// Also fires the <see cref="SelectedAudioStreamChanged"/> event.
    /// </summary>
    public ValueTask<bool> SetSelectedAudioStreamAsync(SelectableMediaStream? stream);
}