using System.Runtime.InteropServices;
using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Raw;
using Sdcb.FFmpeg.Swscales;
using Sdcb.FFmpeg.Utils;

namespace MetadataReader.FFMPEG;

public class FfmpegReader
{
    
    /// <summary>
    /// Get all metadata tags from media from a file path
    /// </summary>
    public static Dictionary<string, string> ReadMetaDataTags(string path)
    {
        var dict = new Dictionary<string, string>();

        using var fCtx = FormatContext.OpenInputUrl(path);

        //get metadata
        var metadata = fCtx.Metadata;
        foreach (var (key, value) in metadata)
        {
            var normalizedKey = key.Replace(" ", "").ToLower();
            dict.Add(normalizedKey, value);
        }

        return dict;
    }

    /// <summary>
    /// Get all metadata tags from media from a byte array
    /// </summary>
    public static Dictionary<string, string> ReadMetaDataTags(byte[] data)
    {
        var dict = new Dictionary<string, string>();

        using var tempMs = new MemoryStream(data);
        using var iOCtx = IOContext.ReadStream(tempMs);
        using var fCtx = FormatContext.OpenInputIO(iOCtx);

        //get metadata
        var metadata = fCtx.Metadata;
        foreach (var (key, value) in metadata)
        {
            var normalizedKey = key.Replace(" ", "").ToLower();
            dict.Add(normalizedKey, value);
        }

        return dict;
    }
    
    /// <summary>
    /// Get the duration of a media file
    /// </summary>
    public static TimeSpan GetDuration(string path)
    {
        using var fCtx = FormatContext.OpenInputUrl(path);
        if (fCtx.Duration > 0)
        {
            return TimeSpan.FromMicroseconds(fCtx.Duration);
        }
        //Duration is not set, try to get it from the streams
        fCtx.LoadStreamInfo();
        if (fCtx.Duration > 0)
        {
            return TimeSpan.FromMicroseconds(fCtx.Duration);
        }
        
        if (fCtx.Streams.All(s => s.Duration <= 0))
            return TimeSpan.Zero;
        
        var streamWithValidDuration = fCtx.Streams.First(s => s.Duration > 0);
        
        var duration = streamWithValidDuration.Duration;
        var timeBase = streamWithValidDuration.TimeBase.ToDouble();
        var durationInSeconds = duration * timeBase;
        return TimeSpan.FromSeconds(durationInSeconds);
    }
    
    //TODO: Add a method to get the duration from a byte array
    //Probably didnt do that cause with YouTube videos its always included in the metadata

    /// <summary>
    /// Try to read cover art from an audio files IDv3 tags or from the same directory
    /// </summary>
    public static async Task<byte[]?> TryReadCoverArt(string path)
    {
        try
        {
            await using var fs = File.OpenRead(path);
            if (TryReadCoverArtNative(fs, out var coverArt))
                return coverArt;

            return await TryFindCoverArt(path);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    /// <summary>
    /// Try to read cover art from an audio files IDv3 tags
    /// </summary>
    public static async Task<byte[]?> TryReadCoverArt(byte[] data)
    {
        try
        {
            await using var ms = new MemoryStream(data);
            if (TryReadCoverArtNative(ms, out var coverArt))
                return coverArt;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    /// <summary>
    /// Get a thumbnail from a video file
    /// </summary>
    public static bool TryGetVideoThumbnail(string path, out byte[] thumbnail)
    {
        using var fs = File.OpenRead(path);
        return TryGetVideoThumbnailInternal(fs, out thumbnail);
    }

    /// <summary>
    /// Get a thumbnail from a video byte array
    /// </summary>
    public static bool TryGetVideoThumbnail(byte[] data, out byte[] thumbnail)
    {
        using var ms = new MemoryStream(data);
        return TryGetVideoThumbnailInternal(ms, out thumbnail);
    }

    /// <summary>
    /// Seeks to 50% of the video and tries to get a thumbnail, gets converted to PNG
    /// </summary>
    private static bool TryGetVideoThumbnailInternal(Stream dataStream, out byte[] thumbnail)
    {
        thumbnail = Array.Empty<byte>();

        using var iOCtx = IOContext.ReadStream(dataStream);
        using var fCtx = FormatContext.OpenInputIO(iOCtx);

        if (fCtx.Streams.All(s => s.Codecpar?.CodecType != AVMediaType.Video))
            return false;

        var stream = fCtx.FindBestStream(AVMediaType.Video);
        if (stream.Codecpar is null)
            return false; //Idk how, but sure


        var codec = Codec.FindDecoderById(stream.Codecpar.CodecId);
        using var codecCtx = new CodecContext(codec);
        codecCtx.FillParameters(stream.Codecpar);
        codecCtx.Open(codec);

        //seek to 50% of the video
        var seekTarget = fCtx.Duration * 0.5;
        var seekTargetTimeBase = stream.TimeBase.ToDouble() * seekTarget;
        fCtx.SeekFrame((long)seekTargetTimeBase, stream.Index);

        using var packet = new Packet();
        using var frame = new Frame();
        bool gotFrame = false;
        while (fCtx.ReadFrame(packet) >= 0)
        {
            if (packet.StreamIndex != stream.Index)
                continue;
            codecCtx.SendPacket(packet);
            codecCtx.ReceiveFrame(frame);
            if (frame.Width <= 0 || frame.Height <= 0)
                continue;
            gotFrame = true;
            break;
        }
        
        if (!gotFrame)
            return false;
        
        var pngCodec = Codec.FindEncoderById(AVCodecID.Png);
        using var pngContext = new CodecContext(pngCodec);
        pngContext.PixelFormat = AVPixelFormat.Rgb24;
        pngContext.Width = frame.Width;
        pngContext.Height = frame.Height;
        pngContext.TimeBase = stream.TimeBase; //or maybe just 1/25??
        pngContext.Open(pngCodec);
        using var tempFrame = Frame.CreateVideo(frame.Width, frame.Height, pngContext.PixelFormat);
        using var converter = new VideoFrameConverter();
        converter.ConvertFrame(frame, tempFrame);
        
        using var pngPacket = new Packet();
        pngContext.SendFrame(tempFrame);
        pngContext.ReceivePacket(pngPacket);
        
        thumbnail = pngPacket.Data.ToArray();
        
        return true;
    }
    
    /// <summary>
    /// Try to read cover art from the IDv3 tags
    /// </summary>
    private static bool TryReadCoverArtNative(Stream data, out byte[]? coverArt)
    {
        coverArt = Array.Empty<byte>();
        using var iOCtx = IOContext.ReadStream(data);
        using var fCtx = FormatContext.OpenInputIO(iOCtx);
        if (fCtx.Streams.All(s => s.Codecpar?.CodecType != AVMediaType.Video))
            return false;
        var stream = fCtx.Streams.First(s => s.Codecpar?.CodecType == AVMediaType.Video);
        if (stream.Codecpar is null)
            return false; //Idk how, but sure

        //Get cover art
        var codec = Codec.FindDecoderById(stream.Codecpar.CodecId);
        using var codecCtx = new CodecContext(codec);
        codecCtx.Open(codec);
        using var packet = new Packet();
        using var frame = new Frame();
        while (fCtx.ReadFrame(packet) >= 0)
        {
            if (packet.StreamIndex != stream.Index)
                continue;
            codecCtx.SendPacket(packet);
            codecCtx.ReceiveFrame(frame);
            if (frame.Width <= 0 || frame.Height <= 0)
                continue;
            
            //Convert to png
            var pngCodec = Codec.FindEncoderById(AVCodecID.Png);
            using var pngContext = new CodecContext(pngCodec);
            pngContext.PixelFormat = AVPixelFormat.Rgb24;
            pngContext.Width = frame.Width;
            pngContext.Height = frame.Height;
            pngContext.TimeBase = stream.TimeBase; //or maybe just 1/25??
            
            pngContext.Open(pngCodec);
            using var tempFrame = Frame.CreateVideo(frame.Width, frame.Height, pngContext.PixelFormat);
            using var converter = new VideoFrameConverter();
            converter.ConvertFrame(frame, tempFrame);
            
            using var pngPacket = new Packet();
            pngContext.SendFrame(tempFrame);
            pngContext.ReceivePacket(pngPacket);
            
            coverArt = pngPacket.Data.ToArray();
            return true;
        }

        return false;
    }
    
    /// <summary>
    /// Try to find cover art in the same directory as the file
    /// Partially Copilot code, could be cleaned up a bit
    /// </summary>
    private static async Task<byte[]?> TryFindCoverArt(string path)
    {
        try
        {
            //try to find cover art, *.jpg, *.jpeg, *.png, *.bmp, name: cover, folder, front or thumbnail
            var dir = Path.GetDirectoryName(path);
            if (dir is null)
                return null;

            var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
            var imageFiles = files.Where(f =>
                f.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase) ||
                f.EndsWith(".jpeg", StringComparison.CurrentCultureIgnoreCase) ||
                f.EndsWith(".png", StringComparison.CurrentCultureIgnoreCase) ||
                f.EndsWith(".bmp", StringComparison.CurrentCultureIgnoreCase));

            var coverArtFiles = imageFiles.Where(f =>
                f.Contains("cover", StringComparison.CurrentCultureIgnoreCase) ||
                f.Contains("folder", StringComparison.CurrentCultureIgnoreCase) ||
                f.Contains("front", StringComparison.CurrentCultureIgnoreCase) ||
                f.Contains("thumbnail", StringComparison.CurrentCultureIgnoreCase));

            var coverArt = coverArtFiles.FirstOrDefault();
            if (coverArt is null)
            {
                //Maybe there is a cover, front, scans or thumbnail folder
                var folders = Directory.GetDirectories(dir, "*.*", SearchOption.TopDirectoryOnly);
                var coverArtFolders = folders.Where(f =>
                    f.Contains("cover", StringComparison.CurrentCultureIgnoreCase) ||
                    f.Contains("scans", StringComparison.CurrentCultureIgnoreCase) ||
                    f.Contains("front", StringComparison.CurrentCultureIgnoreCase) ||
                    f.Contains("thumbnail", StringComparison.CurrentCultureIgnoreCase));

                foreach (var folder in coverArtFolders)
                {
                    var folderFiles = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
                    var folderImageFiles = folderFiles.Where(f =>
                        f.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase) ||
                        f.EndsWith(".jpeg", StringComparison.CurrentCultureIgnoreCase) ||
                        f.EndsWith(".png", StringComparison.CurrentCultureIgnoreCase) ||
                        f.EndsWith(".bmp", StringComparison.CurrentCultureIgnoreCase));

                    coverArt = folderImageFiles.FirstOrDefault();
                }

                if (coverArt is null) //No cover art found
                    return null;
            }

            var imgBytes = await File.ReadAllBytesAsync(coverArt);
            return imgBytes;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }
}