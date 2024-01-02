// See https://aka.ms/new-console-template for more information

using Manager.Services.Data;
using Manager.Services.VlcVideo;

Console.WriteLine("Hello, World!");

var fPath = "C:\\Users\\Sekoree\\Music\\iTunes\\iTunes Media\\Music\\MAD MEDiCiNE\\Mad MEDiCATiON\\05 Minzai.m4a";
var fPathNoArt = "C:\\Users\\Sekoree\\Music\\lolita [1666106175].mp3";
var vidPath = "G:\\Anime\\The iDOLM@STER Movie Kagayaki no Mukougawa e.mkv";
var smallVidPath = "C:\\Users\\Sekoree\\Videos\\hartkoer_tenokk.mov";


//var coverArt = MetaDataReader.TryReadCoverArt(fPath);
//var coverArtNoArt = MetaDataReader.TryReadCoverArt(fPathNoArt);

//var fileTags = MetaDataReader.ReadMetaDataTags(fPath);
//var tags = MetaDataReader.ReadMetaDataTags(data);

var d = new LocalDataService("C:", "Local", 0);

var item = await d.GetPlayItemAsync(smallVidPath);
if (item is null)
{
    Console.WriteLine("Item is null");
    return;
}

await d.CachePlayItemAsync(item);

var v = new LibVlcVideoBackendService("VLC", 0);
await v.InitializeAsync("--no-video");

var channel = await v.CreateChannelAsync(item, null);
if (channel is null)
{
    Console.WriteLine("Channel is null");
    return;
}

var could = await v.PlayChannelAsync(channel);
if (!could)
{
    Console.WriteLine("Could not play");
    return;
}

//await v.StopChannelAsync(channel);

Console.ReadLine();

//var item = await d.GetPlayItemAsync(fPath);
//if (item is null)
//{
//    Console.WriteLine("Item is null");
//    return;
//}

//Console.ReadLine();