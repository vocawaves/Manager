// See https://aka.ms/new-console-template for more information

using HeyRed.Mime;
using Manager.Services.Utilities;

Console.WriteLine("Hello, World!");

var fPath = "C:\\Users\\Sekoree\\Music\\iTunes\\iTunes Media\\Music\\MAD MEDiCiNE\\Mad MEDiCATiON\\05 Minzai.m4a";
var fPathNoArt = "C:\\Users\\Sekoree\\Music\\lolita [1666106175].mp3";
var vidPath = "G:\\Anime\\The iDOLM@STER Movie Kagayaki no Mukougawa e.mkv";

//var data = await File.ReadAllBytesAsync(fPath);

var thumb = MetaDataReader.TryGetVideoThumbnail(vidPath, out var data);

var format = MimeGuesser.GuessFileType(data);

File.WriteAllBytes($"thumb.{format.Extension}", data);

//var coverArt = MetaDataReader.TryReadCoverArt(fPath);
//var coverArtNoArt = MetaDataReader.TryReadCoverArt(fPathNoArt);

//var fileTags = MetaDataReader.ReadMetaDataTags(fPath);
//var tags = MetaDataReader.ReadMetaDataTags(data);

//var d = new LocalDataService("Local", 0);
//var b = new BassAudioBackendService("Bass", 0);
//var item = await d.GetPlayItemAsync(fPath);
//if (item is null)
//{
//    Console.WriteLine("Item is null");
//    return;
//}

//Console.ReadLine();