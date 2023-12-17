// See https://aka.ms/new-console-template for more information

using Manager.Services.BassAudio;
using Manager.Services.Data;

Console.WriteLine("Hello, World!");

var fPath = "C:\\Users\\Sekoree\\Music\\iTunes\\iTunes Media\\Music\\MAD MEDiCiNE\\Mad MEDiCATiON\\05 Minzai.m4a";

var d = new LocalDataService("Local", 0);
var b = new BassAudioBackendService("Bass", 0);
var bCould = await b.InitializeAsync("-f", "96000");
if (!bCould)
{
    Console.WriteLine("Bass could not initialize");
    return;
}

var item = await d.GetPlayItemAsync(fPath);
if (item is null)
{
    Console.WriteLine("Item is null");
    return;
}
var cacheResult = await d.CachePlayItemAsync(item);
if (!cacheResult)
{
    Console.WriteLine("Cache failed");
    return;
}

var ad = await b.GetDevicesAsync();
var channel = await b.CreateChannelAsync(item, volume: 0.015f, frequency: 48000, onEnded: playItem =>
{
    Console.WriteLine($"{playItem.Title} ended");
});
if (channel is null)
{
    Console.WriteLine("Channel is null");
    return;
}

var playResult = await channel.PlayAsync();
if (playResult is false)
{
    Console.WriteLine("Play failed");
    return;
}

Console.WriteLine("Playing");

Console.ReadLine();