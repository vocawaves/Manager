using Manager.Shared.Entities;
using Manager.Shared.Interfaces;

namespace Manager.Shared.Delegates;

#region Generic

public delegate void ManagerComponentErrorEventHandler(ManagerComponent sender, string message, Exception? exception = null);

#endregion

#region Audio Service

public delegate void AudioServiceGlobalDeviceChangedEventHandler(IAudioBackendService sender, AudioDevice device);
public delegate void AudioServiceChannelCreatedEventHandler(IAudioBackendService sender, AudioChannel channel);
public delegate void AudioServiceChannelDestroyedEventHandler(IAudioBackendService sender, AudioChannel channel);

public delegate void AudioServiceChannelVolumeChangedEventHandler(IAudioBackendService sender, AudioChannel channel, float volume);
public delegate void AudioServiceChannelStateChangedEventHandler(IAudioBackendService sender, AudioChannel channel, ChannelState state);
public delegate void AudioServiceChannelPositionChangedEventHandler(IAudioBackendService sender, AudioChannel channel, double positionMs);

#endregion