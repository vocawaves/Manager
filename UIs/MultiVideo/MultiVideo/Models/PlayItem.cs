using System.Threading.Tasks;
using Manager.Shared.Interfaces.Audio;
using Manager.Shared.Interfaces.Video;
using ValueTaskSupplement;

namespace MultiVideo.Models;

public class PlayItem
{
    public IAudioChannel MainAudio { get; set; }

    public IVideoChannel MainVideo { get; set; }

    public IVideoChannel SecondaryVideo { get; set; }

    public bool PlayNextItem { get; set; } = true;

    public PlayItem(IAudioChannel mainAudio, IVideoChannel mainVideo, IVideoChannel secondaryVideo)
    {
        MainAudio = mainAudio;
        MainVideo = mainVideo;
        SecondaryVideo = secondaryVideo;
    }
    
    public EndCondition EndCondition { get; set; } = EndCondition.MainAudioEnd;
    
    public async ValueTask PlayAsync()
    {
        await ValueTaskEx.WhenAll(MainVideo.PlayAsync(), SecondaryVideo.PlayAsync());
        await MainAudio.PlayAsync();
    }

    public async ValueTask StopAsync()
    {
        await ValueTaskEx.WhenAll(MainVideo.StopAsync(), SecondaryVideo.StopAsync());
        await MainAudio.StopAsync();
    }
}

public enum EndCondition
{
    MainAudioEnd,
    MainVideoEnd,
    SecondaryVideoEnd
}