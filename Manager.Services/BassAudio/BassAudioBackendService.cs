using System.Reflection;
using System.Runtime.InteropServices;
using ManagedBass;
using Manager.Shared;
using Manager.Shared.Delegates;
using Manager.Shared.Entities;
using Manager.Shared.Enums;
using Manager.Shared.Interfaces;

namespace Manager.Services.BassAudio;

public class BassAudioBackendService : ManagerComponent, IAudioBackendService
{
    public event AudioServiceGlobalDeviceChangedEventHandler? GlobalDeviceChanged;
    public event AudioServiceChannelCreatedEventHandler? ChannelCreated;
    
    public BassAudioBackendService(string name, ulong parent) : base(name, parent)
    {
    }
    
    public override ValueTask<bool> InitializeAsync(params string[] options)
    {
        Bass.Init();
        //options for update period n stuff?
        if (options.Contains("-up"))
        {
            var upIndex = Array.IndexOf(options, "-up");
            var value = int.Parse(options[upIndex + 1]);
            Bass.UpdatePeriod = value;
        }

        return new(true);
    }
    
    public ValueTask<AudioDevice[]> GetDevicesAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<AudioDevice> GetCurrentlySelectedDeviceAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetDeviceAsync(AudioDevice device)
    {
        throw new NotImplementedException();
    }

    public ValueTask<AudioDevice> GetChannelDeviceAsync(IAudioChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetChannelDeviceAsync(IAudioChannel channel, AudioDevice device)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IAudioChannel?> CreateChannelAsync(PlayItem playItem, Action<PlayItem>? onEnded = null)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IAudioChannel?> CreateChannelAsync(PlayItem playItem, float volume = 1, Action<PlayItem>? onEnded = null)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IAudioChannel?> CreateChannelAsync(PlayItem playItem, int frequency = 44100, float volume = 1, Action<PlayItem>? onEnded = null)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IAudioChannel?> CreateChannelAsync(PlayItem playItem, AudioDevice? device = null, int frequency = 44100, float volume = 1,
        Action<PlayItem>? onEnded = null)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> DestroyChannelAsync(IAudioChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<float> GetChannelVolumeAsync(IAudioChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetChannelVolumeAsync(IAudioChannel channel, float volume)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> PlayChannelAsync(IAudioChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> PauseChannelAsync(IAudioChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> ResumeChannelAsync(IAudioChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> StopChannelAsync(IAudioChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ChannelState> GetChannelStateAsync(IAudioChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetChannelStateAsync(IAudioChannel channel, ChannelState state)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TimeSpan> GetChannelPositionAsync(IAudioChannel channel)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> SetChannelPositionAsync(IAudioChannel channel, double positionMs)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TimeSpan> GetChannelLengthAsync(IAudioChannel channel)
    {
        throw new NotImplementedException();
    }

    static BassAudioBackendService()
    {
        //Set resolver for BASS since its in the "Natives" folder
        NativeLibrary.SetDllImportResolver(typeof(Bass).Assembly,
            (name, assembly, path) =>
                NativeLibrary.Load(Path.Combine(Directory.GetCurrentDirectory(), "Natives", name)));
    }
}