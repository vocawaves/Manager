using Manager.BassPlayer;
using Manager.LocalDataService;
using Manager.Shared.Entities;
using Manager.Shared.Interfaces.General;
using Microsoft.Extensions.Logging;

namespace SandBox;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var lf = LoggerFactory.Create(x => x.SetMinimumLevel(LogLevel.Debug).AddConsole());
        var b = new BassBackend(lf, "default", 0);
        var d = new FileDataService(lf, "default", "default", 0);
        await b.InitializeAsync();
        
        var allArgsExist = args.All(File.Exists);
        if (!allArgsExist)
        {
            Console.WriteLine("Not all files exist");
            return;
        }

        foreach (var mediaFile in args)
        {
            var pi = await d.GetPlayItemFromUriAsync(mediaFile);
            if (pi is null)
            {
                Console.WriteLine($"Failed to get playback item for {mediaFile}");
                continue;
            }

            var couldCache = await d.CachePlayItemAsync(pi);
            if (!couldCache)
            {
                Console.WriteLine($"Failed to cache {mediaFile}");
                continue;
            }
            
            var channel = await b.CreateChannelAsync(pi);
            if (channel is null)
            {
                Console.WriteLine($"Failed to create channel for {mediaFile}");
                continue;
            }

            channel.Playing += (sender, args) =>
            {
                var pi = (IMediaChannel) sender;
                Console.WriteLine($"Playing {pi.PlaybackItem.Title} by {pi.PlaybackItem.Artist} ({pi.PlaybackItem.Duration})");
                return ValueTask.CompletedTask;
            };
            
            var tcs = new TaskCompletionSource<bool>();
            //set true when the channel fires the PlaybackFinished event
            channel.Ended += (sender, args) =>
            {
                tcs.SetResult(true);
                return ValueTask.CompletedTask;
            };
            
            await channel.PlayAsync();
            await tcs.Task;
            await channel.DisposeAsync();
            await d.RemoveFromCacheAsync(pi);
        }
    }
}