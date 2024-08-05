// See https://aka.ms/new-console-template for more information

using Manager.DataBackends.Local;
using Manager.MediaBackends.BassPlayer;
using Manager.MediaBackends.LibVLCPlayer;
using Manager.Shared;
using Manager.Shared.Cache;
using Manager.Shared.Entities;
using Manager.Shared.Extensions;
using Manager.Shared.Interfaces.Audio;
using Microsoft.Extensions.Logging;
using ValueTaskSupplement;

Console.WriteLine("Hello, World!");

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug);
    //console with loglevel debug
    builder.AddConsole();
});

var logger = loggerFactory.CreateLogger<Program>();
var componentManager = new ComponentManager(loggerFactory);

var dummyCache = componentManager.CreateComponent<DummyCacheStrategy>("DummyCache", 0);
if (dummyCache is null)
{
    logger.LogError("Failed to create cache strategy");
    return;
}

var localDataConfig = new LocalDataServiceConfiguration
{
    CacheStrategy = dummyCache
};
var localDataService =
    componentManager.CreateComponent<LocalDataService, LocalDataServiceConfiguration>("LocalDataService", 0,
        localDataConfig);

if (localDataService is null)
{
    logger.LogError("Failed to create LocalDataService");
    return;
}

var vlcBackend = componentManager.CreateComponent<LibVLCBackend>("VlcBackend", 0);
if (vlcBackend is null)
{
    logger.LogError("Failed to create VLC backend");
    return;
}

var init = await vlcBackend.InitializeAsync();
if (!init)
{
    logger.LogError("Failed to initialize VLC backend");
    return;
}

var bassBackend = componentManager.CreateComponent<BassBackend>("BassBackend", 0);
if (bassBackend is null)
{
    logger.LogError("Failed to create Bass backend");
    return;
}

var initBass = await bassBackend.InitializeAsync();
if (!initBass)
{
    logger.LogError("Failed to initialize Bass backend");
    return;
}
        
var mediaItem = await localDataService.GetMediaItemAsync("C:\\Users\\Sekoree\\Videos\\11 - Shinjidai(1).mp4");
if (mediaItem is null)
{
    logger.LogError("Failed to get media item");
    return;
}

var cached = await mediaItem.CacheAsync();

var channel = await vlcBackend.CreateChannelAsync(mediaItem);
if (channel is null)
{
    logger.LogError("Failed to create channel");
    return;
}

var channel2 = await vlcBackend.CreateChannelAsync(mediaItem);
if (channel2 is null)
{
    logger.LogError("Failed to create channel 2");
    return;
}

var channel3 = await bassBackend.CreateChannelAsync(mediaItem);
if (channel3 is null)
{
    logger.LogError("Failed to create channel 3");
    return;
}

//mute channel 1 and 2
if (channel is IAudioChannel audioChannel && channel2 is IAudioChannel audioChannel2)
{
    await audioChannel.SetVolumeAsync(0);
    await audioChannel2.SetVolumeAsync(0);
}

var testScene = componentManager.CreateComponent<Scene>("TestScene", 0);
if (testScene is null)
{
    logger.LogError("Failed to create test scene");
    return;
}

await ValueTaskEx.WhenAll(
    channel.PlayAsync(),
    channel2.PlayAsync()
);
await channel3.PlayAsync();

//await Task.Delay(5000);
//
//await ValueTaskEx.WhenAll(
//    channel.StopAsync(),
//    channel2.StopAsync(),
//    channel3.StopAsync()
//);
//
//await Task.Delay(5000);
//
//await ValueTaskEx.WhenAll(
//    channel.PlayAsync(),
//    channel2.PlayAsync(),
//    channel3.PlayAsync()
//);

await Task.Delay(-1);