// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Local;
using Manager.MediaBackends;
using Manager2.Shared;
using Microsoft.Extensions.Logging;
using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Toolboxs.Extensions;
using Sdcb.FFmpeg.Utils;
using LogLevel = Sdcb.FFmpeg.Raw.LogLevel;

Console.WriteLine("Hello, World!");

//FFmpegLogger.LogLevel = LogLevel.Debug;

var startTime = Stopwatch.GetTimestamp();

var loggingFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
});
LoggingHelper.SetLoggerFactory(loggingFactory);

var programArch = Environment.Is64BitProcess ? "x64" : "x86";
Console.WriteLine($"Program architecture: {programArch}");

var cachePath = Path.Combine(Directory.GetCurrentDirectory(), "cache");
var f = new LocalDataService(cachePath, loggingFactory.CreateLogger<LocalDataService>());
var b = new BassPlayer("BassPlayer", loggingFactory.CreateLogger<BassPlayer>());

var couldInit = await b.InitializeAsync();
if (!couldInit)
{
    Console.WriteLine("Failed to initialize BassPlayer");
    return;
}

var me = await f.GetMediaItemByPathAsync("C:\\Users\\Sekoree\\Music\\IA_05 -SHINE-\\01_02_残響ディスタンス (feat. hirihiri & lilbesh ramko).flac");
if (me == null)
{
    Console.WriteLine("Media item is null");
    return;
}

//var couldCache = await me.CacheAsync();
//if (!couldCache)
//{
//    Console.WriteLine("Failed to cache media item");
//    return;
//}

//var ms = await me.DefaultAudioStream!.ExtractStreamAsync();
//if (!ms)
//{
//    Console.WriteLine("Memory stream is null");
//    return;
//}

var channel = await b.CreateMediaChannelAsync(me.DefaultAudioStream!);
if (channel == null)
{
    Console.WriteLine("Channel is null");
    return;
}

await channel.PlayAsync();

var endTime = Stopwatch.GetTimestamp();
var ts = TimeSpan.FromSeconds((endTime - startTime) / (double)Stopwatch.Frequency);
Console.WriteLine($"Time: {ts}");
Console.ReadLine();

//var timeStart = Stopwatch.GetTimestamp();
//var filePath = Path.Combine(Directory.GetCurrentDirectory(), "output.mp4");
//using var inFc = FormatContext.OpenInputUrl("E:\\IMAS_Content\\MKVs\\THE IDOLM@STER MOVIE\\THE IDOLM@STER MOVIE.mkv");
////using var inFc = FormatContext.OpenInputUrl(args[0].StartsWith("\"") ? args[0][1..^1] : args[0]);
////using var inFc = FormatContext.OpenInputUrl("E:\\IMAS_Content\\MKVs\\THE IDOLM@STER MOVIE\\THE IDOLM@STER MOVIE.mkv");
////using var inFc = FormatContext.OpenInputUrl("C:\\Users\\Sekoree\\Music\\Molly - BAGS.flac");
////using var inFc = FormatContext.OpenInputUrl("C:\\Users\\Sekoree\\Music\\Jiyagi-feat.-Kid-Berry-Ohne-Dich-v2.mp3");
////using var inFc = FormatContext.OpenInputUrl("C:\\Users\\Sekoree\\Music\\iTunes\\iTunes Media\\Music\\Akari24\\Crack Cat - Single\\01 Crack Cat.m4a");
////using var inFc = FormatContext.OpenInputUrl("C:\\Users\\Sekoree\\Music\\Third Rail - mekaloton.wav");
//inFc.LoadStreamInfo();
//var inAudioStream = inFc.GetVideoStream();
//using var audioDecoder = new CodecContext(Codec.FindDecoderById(inAudioStream.Codecpar!.CodecId));
//audioDecoder.FillParameters(inAudioStream.Codecpar);
//audioDecoder.Open();
//audioDecoder.ChLayout = inAudioStream.Codecpar!.ChLayout;
//
//using var outFc = FormatContext.AllocOutput(fileName: filePath, formatName: "mp4");
////var outCodec = Codec.FindEncoderByName("pcm_s16le");
//var outCodec = Codec.FindEncoderById(audioDecoder.CodecId);
//outFc.VideoCodec = outCodec;
//var outAudioStream = outFc.NewStream(outCodec);
//using var audioEncoder = new CodecContext(outCodec);
////audioEncoder.ChLayout = audioDecoder.ChLayout;
////audioEncoder.SampleFormat = outFc.AudioCodec!.Value.NegociateSampleFormat(AVSampleFormat.S16);
////audioEncoder.SampleRate = outFc.AudioCodec!.Value.NegociateSampleRates(audioDecoder.SampleRate);
//audioEncoder.PixelFormat = (AVPixelFormat)inAudioStream.Codecpar.Format;
//audioEncoder.Height = inAudioStream.Codecpar.Height;
//audioEncoder.Width = inAudioStream.Codecpar.Width;
//audioEncoder.TimeBase = inAudioStream.TimeBase;
//audioEncoder.Open(outFc.VideoCodec);
//outAudioStream.Codecpar!.CopyFrom(audioEncoder);
//
//using var io = IOContext.OpenWrite(filePath);
//outFc.Pb = io;
//outFc.WriteHeader();
//MediaThreadQueue<Frame> decodingQueue =
//    inFc.ReadPackets(inAudioStream.Index).DecodeAllPackets(inFc, videoCodec: audioDecoder).ToThreadQueue(boundedCapacity: 256);
//MediaThreadQueue<Packet> encodingQueue = decodingQueue.GetConsumingEnumerable().ConvertFrames(audioEncoder)
//        .EncodeFrames(audioEncoder).ToThreadQueue( boundedCapacity: 256);
//encodingQueue.GetConsumingEnumerable().WriteAll(outFc);
//outFc.WriteTrailer();
//var timeEnd = Stopwatch.GetTimestamp();
//var ts = TimeSpan.FromSeconds((timeEnd - timeStart) / (double)Stopwatch.Frequency);
//Console.WriteLine($"Time: {ts}");
//Console.WriteLine("Done");