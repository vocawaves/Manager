using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces;

namespace Manager.Shared.Delegates;

#region Generic

public delegate void ManagerComponentErrorEventHandler(ManagerComponent sender, string message, Exception? exception = null);

#endregion

#region Audio Service

public delegate void AudioServiceGlobalDeviceChangedEventHandler(IAudioBackendService sender, AudioDevice device);
public delegate void AudioServiceChannelCreatedEventHandler(IAudioBackendService sender, IAudioChannel channel);
public delegate void AudioServiceChannelDestroyedEventHandler(IAudioBackendService sender, IAudioChannel channel);

public delegate void AudioServiceChannelVolumeChangedEventHandler(IAudioBackendService sender, IAudioChannel channel, float volume);
public delegate void AudioServiceChannelStateChangedEventHandler(IAudioBackendService sender, IAudioChannel channel, ChannelState state);
public delegate void AudioServiceChannelPositionChangedEventHandler(IAudioBackendService sender, IAudioChannel channel, TimeSpan position);

#endregion