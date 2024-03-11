using Manager.BassPlayer;
using Manager.LocalDataService;
using Manager.Shared.Entities;
using Manager.Shared.Interfaces.Data;
using Manager.Shared.Interfaces.General;
using Manager.YouTubeDataService;
using Microsoft.Extensions.Logging;

namespace SandBox;

internal class Program
{
    public static async Task Main(string[] args)
    {
        //args = new[] { "C:\\Users\\Sekoree\\Music\\2017.10.15 [IO-0311] 東方氷雪大感謝 [秋例大祭4]\\(01) [IOSYS] チルノのパーフェクトさんすう教室 \u2468周年バージョン.flac" };
        var lf = LoggerFactory.Create(builder => builder.AddConsole());
        var dataService = new LocalDataService(lf, "Basic", 0, "C:\\");
        var ytService = new YouTubeDataService(lf, "YouTube", 0);
        var audioBackend = new BassBackend(lf, "Bass", 0);
        await audioBackend.InitializeAsync();
        
        //var ai = await dataService.GetAudioItemAsync(args[0]);
        IAudioDataSource serviceToUse;
        if (File.Exists(args[0]))
            serviceToUse = dataService;
        else 
            serviceToUse = ytService;
        
        var ai = await serviceToUse.GetAudioItemAsync("https://www.youtube.com/watch?v=3eM6quLZxMg");
        if (ai is null)
        {
            Console.WriteLine("Failed to get audio item");
            return;
        }
        
        var couldCache = await ai.CacheAsync();
        if (!couldCache)
        {
            Console.WriteLine("Failed to cache audio item");
            return;
        }
        
        var channel = await audioBackend.CreateChannelAsync(ai);
        if (channel is null)
        {
            Console.WriteLine("Failed to create channel");
            return;
        }
        
        channel.Ended += async (sender, e) =>
        {
            Console.WriteLine("Channel ended");
            await channel.DisposeAsync();
            await ai.RemoveFromCacheAsync();
            Environment.Exit(0);
        };
        await channel.PlayAsync();
        await Task.Delay(-1);
    }
}