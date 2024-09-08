using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Timers;
using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Raw;
using Sdcb.FFmpeg.Swscales;
using Sdcb.FFmpeg.Toolboxs.Extensions;
using Sdcb.FFmpeg.Utils;
using Timer = System.Timers.Timer;

namespace FV;

public class VideoDecoder : IDisposable
{
    private byte_ptrArray4 _targetData;
    public IntPtr TargetDataFirst => _targetData[0];
    private int_array4 _targetLineSize;
    public int TargetLineSizeFirst => _targetLineSize[0];

    public FormatContext? FormatContext { get; set; }
    public MediaStream? VideoStream { get; set; }
    public CodecContext? CodecContext { get; set; }
    public PixelConverter? SwsContext { get; set; }

    public TimeSpan Duration { get; set; }
    public long Bitrate { get; set; }
    public double FrameRate { get; set; }

    public long TotalFrames { get; set; }
    public long CurrentFrame { get; set; } = 0;
    public long SkippedFrames { get; set; } = 0;

    public int FrameHeight { get; set; }
    public int FrameWidth { get; set; }
    public TimeSpan FrameDuration { get; set; }
    public IntPtr BufferPtr { get; set; }

    public Frame? MainFrame { get; set; }
    public Packet? MainPacket { get; set; }


    public Action? OnFrameDecoded { get; set; }

    public System.Timers.Timer? DecodeTimer { get; private set; }
    public bool IsPlaying { get; private set; } = false;
    private long _startTimestamp = 0;


    public bool InitializeFromFile(string path)
    {
        FormatContext = FormatContext.OpenInputUrl(path);
        if (FormatContext == null)
            throw new Exception("Failed to open input file");

        FormatContext.LoadStreamInfo();
        VideoStream = FormatContext.FindBestStreamOrNull(AVMediaType.Video);
        if (VideoStream == null || VideoStream.Value.Codecpar == null)
            throw new Exception("Failed to find video stream");
        var codec = Codec.FindDecoderById(VideoStream.Value.Codecpar.CodecId);
        CodecContext = new CodecContext(codec);
        CodecContext.FillParameters(VideoStream.Value.Codecpar);
        CodecContext.Open(codec);

        var durInSec = VideoStream.Value.GetDurationInSeconds();
        Duration = TimeSpan.FromSeconds(durInSec);
        Bitrate = VideoStream.Value.Codecpar.BitRate;
        FrameRate = VideoStream.Value.AvgFrameRate.ToDouble();
        TotalFrames = VideoStream.Value.NbFrames;
        FrameHeight = VideoStream.Value.Codecpar.Height;
        FrameWidth = VideoStream.Value.Codecpar.Width;
        FrameDuration = double.IsNaN(FrameRate) ? TimeSpan.Zero : TimeSpan.FromSeconds(1 / FrameRate);

        SwsContext = new PixelConverter(FrameWidth, FrameHeight, CodecContext.PixelFormat, FrameWidth, FrameHeight,
            AVPixelFormat.Bgr0);
        var bufferSize = ffmpeg.av_image_get_buffer_size(AVPixelFormat.Bgr0, FrameWidth, FrameHeight, 1);
        BufferPtr = Marshal.AllocHGlobal(bufferSize);
        _targetData = new byte_ptrArray4();
        _targetLineSize = new int_array4();
        unsafe
        {
            var error = ffmpeg.av_image_fill_arrays(ref _targetData, ref _targetLineSize, (byte*)BufferPtr,
                AVPixelFormat.Bgr0, FrameWidth, FrameHeight, 1);
            if (error < 0)
                throw new Exception("Failed to fill arrays");
        }

        MainFrame = new Frame();
        MainPacket = new Packet();
        DecodeTimer = new Timer(TimeSpan.FromMilliseconds(1));
        DecodeTimer.Elapsed += PushFrame;
        return true;
    }
    
    private object _lock = new object();
    private void PushFrame(object? sender, ElapsedEventArgs args)
    {
        if (!IsPlaying)
            return;
        var shouldBeFrame = (long)((Stopwatch.GetTimestamp() - _startTimestamp) / (Stopwatch.Frequency / FrameRate));
        if (shouldBeFrame == CurrentFrame)
            return;
        CurrentFrame = shouldBeFrame;
        lock (_lock)
        {
            try
            {
                MainFrame!.Unref();
                while (true)
                {
                    MainPacket!.Unref();
                    var packetResult = FormatContext!.ReadFrame(MainPacket);
                    if (packetResult is CodecResult.EOF or < 0)
                    {
                        Stop();
                        return;
                    }

                    if (MainPacket.StreamIndex != VideoStream!.Value.Index)
                        continue;
                    try
                    {
                        CodecContext!.SendPacket(MainPacket);
                        packetResult = CodecContext.ReceiveFrame(MainFrame);
                        if (packetResult is CodecResult.EOF or < 0)
                            return;

                        unsafe
                        {
                            packetResult = (CodecResult)ffmpeg.sws_scale(SwsContext!, MainFrame.Data.ToRawArray(),
                                MainFrame.Linesize.ToArray(), 0, FrameHeight, _targetData.ToRawArray(),
                                _targetLineSize.ToArray());
                            if (packetResult < 0)
                            {
                                return;
                            }
                        }

                        OnFrameDecoded?.Invoke();
                        return;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
    public void CopyToBitmap(IntPtr address, int rowBytes)
    {
        for (var i = 0; i < FrameHeight; i++)
        {
            unsafe
            {
                Unsafe.CopyBlock((address + rowBytes * i).ToPointer(),
                    ((byte*)_targetData[0] + i * _targetLineSize[0]),
                    (uint)_targetLineSize[0]);
            }
        }
    }

    public void Start()
    {
        if (IsPlaying)
            return;
        IsPlaying = true;
        _startTimestamp = Stopwatch.GetTimestamp();
        DecodeTimer!.Start();
    }

    public void Stop()
    {
        if (!IsPlaying)
            return;
        IsPlaying = false;
        DecodeTimer!.Stop();
    }

    public void Dispose()
    {
        MainFrame?.Dispose();
        MainPacket?.Dispose();
        CodecContext?.Dispose();
        FormatContext?.Dispose();
        SwsContext?.Dispose();
        Marshal.FreeHGlobal(BufferPtr);
        DecodeTimer?.Dispose();
    }
}