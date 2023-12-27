using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces;

namespace Manager.Shared.Delegates;

#region Generic

public delegate void ManagerComponentErrorEventHandler(ManagerComponent sender, string message, Exception? exception = null);

#endregion

#region Audio Service

public delegate void AudioServiceGlobalDeviceChangedEventHandler(IAudioBackendService sender, AudioDevice device);
public delegate void BackendServiceChannelCreatedEventHandler(IAudioBackendService sender, IMediaChannel channel);
public delegate void BackendServiceChannelDestroyedEventHandler(IAudioBackendService sender, IMediaChannel channel);

public delegate void AudioServiceChannelVolumeChangedEventHandler(IAudioBackendService sender, IMediaChannel channel, float volume);
public delegate void BackendServiceChannelStateChangedEventHandler(IAudioBackendService sender, IMediaChannel channel, ChannelState state);
public delegate void BackendServiceChannelPositionChangedEventHandler(IAudioBackendService sender, IMediaChannel channel, TimeSpan position);

#endregion