using System.Diagnostics;
using Manager.BassPlayer;
using Manager.LocalDataService;
using Manager.Shared.Interfaces.Data;
using Manager.YouTubeDataService;
using Microsoft.Extensions.Logging;

namespace SandBox;

internal class Program
{
    public static async Task Main(string[] args)
    {
        //args = new[] { "C:\\Users\\Sekoree\\Music\\2017.10.15 [IO-0311] 東方氷雪大感謝 [秋例大祭4]\\(01) [IOSYS] チルノのパーフェクトさんすう教室 \u2468周年バージョン.flac" };
        var lf = LoggerFactory.Create(builder => builder.AddConsole());
        
        var logger = lf.CreateLogger<Program>();
        
        var dataService = new LocalDataService(lf, "Basic", 0, "C:\\");
        var ytService = new YouTubeDataService(lf, "YouTube", 0);
        var audioBackend = new BassBackend(lf, "Bass", 0);
        await audioBackend.InitializeAsync();
        
        foreach (var arg in args)
        {
            var startTime = Stopwatch.GetTimestamp();
            IAudioDataSource serviceToUse;
            if (File.Exists(arg))
                serviceToUse = dataService;
            else 
                serviceToUse = ytService;
        
            var ai = await serviceToUse.GetAudioItemAsync(arg);
            if (ai is null)
            {
                Console.WriteLine("Failed to get audio item");
                return;
            }
            
            ai.CacheStateChanged += (sender, args) =>
            {
                logger.LogInformation("Cache state changed to {State}", args.State);
                return ValueTask.CompletedTask;
            };
            ai.CacheProgressChanged += (sender, args) =>
            {
                logger.LogInformation("Cache progress changed to {Progress}", args.Progress);
                return ValueTask.CompletedTask;
            };
        
            var couldCache = await ai.CacheAsync();
            if (!couldCache)
            {
                Console.WriteLine("Failed to cache audio item");
                return;
            }
        
            var chanCreation = Stopwatch.GetTimestamp();
            var channel = await audioBackend.CreateChannelAsync(ai);
            if (channel is null)
            {
                Console.WriteLine("Failed to create channel");
                return;
            }
            

            var tcs = new TaskCompletionSource<bool>();
            channel.Ended += (sender, _) =>
            {
                tcs.SetResult(true);
                return ValueTask.CompletedTask;
            };
            var playTime = Stopwatch.GetTimestamp();
            await channel.PlayAsync();
            var endTime = Stopwatch.GetTimestamp();
            var timeSpent = TimeSpan.FromTicks(endTime - startTime);
            var timeSpentCreate = TimeSpan.FromTicks(endTime - chanCreation);
            var timeSpentPlay = TimeSpan.FromTicks(endTime - playTime);
            logger.LogInformation("Time spent getting track ready and playing: {TimeSpent}", timeSpent);
            logger.LogInformation("Time spent creating channel and playing: {TimeSpentCreate}", timeSpentCreate);
            logger.LogInformation("Time spent playing: {TimeSpentPlay}", timeSpentPlay);
            await tcs.Task;
            await channel.DisposeAsync();
            await ai.DisposeAsync();
        }
    }
}