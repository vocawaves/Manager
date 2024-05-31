// See https://aka.ms/new-console-template for more information

using Manager.Shared;
using Manager.SimplePlayer;
using Microsoft.Extensions.Logging;

Console.WriteLine("Hello, World!");

//var loggerFactory = LoggerFactory.Create(builder =>
//{
//    builder.AddConsole();
//    builder.SetMinimumLevel(LogLevel.Debug);
//});
//var logger = loggerFactory.CreateLogger<Program>();
//var instancer = new ComponentManager(loggerFactory);
//
//var mediaPlayer = instancer.CreateManagerComponent<MediaPlayer>("MediaPlayer", 0);
//if (mediaPlayer == null)
//{
//    logger.LogError("Failed to create media player.");
//    return;
//}
//await mediaPlayer.InitializeAsync();
//
//var timer = new System.Timers.Timer(1000);
//timer.Elapsed += async (sender, args) =>
//{
//    if (mediaPlayer.ActiveMediaChannel == null) 
//        return;
//    var position = await mediaPlayer.ActiveMediaChannel.GetPositionAsync();
//    logger.LogInformation("Position: {Position}", position);
//};
//
//var couldAdd = await mediaPlayer.AddMediaAsync("C:\\Users\\Sekoree\\Music\\BABYMETAL, Electric Callboy - 01. RATATATA (Explicit).flac");
//if (couldAdd != null)
//    logger.LogInformation("Added media");
//else
//    logger.LogError("Failed to add media");
//
//var item = mediaPlayer.StoredMedia.FirstOrDefault();
//if (item == null)
//{
//    logger.LogError("No media item found.");
//    return;
//}
//timer.Start();
//var couldPlay = await mediaPlayer.PlayAsync(item);
//if (couldPlay)
//    logger.LogInformation("Playing media");
//else
//    logger.LogError("Failed to play media");
//
//Console.WriteLine("End");
//Console.ReadLine();