using System.Runtime.InteropServices;
using System.Timers;
using FFmpeg.AutoGen;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace FFMPEGPlayer;

public unsafe class VideoDecoder
{
    private readonly ILogger<VideoDecoder>? _logger;
    
    private AVFormatContext* _formatContext = null;
    private int _videoStreamIndex;
    private AVStream* _videoStream;
    private AVCodecContext* _codecContext;
    private AVCodecID _codecId;
    private string? _codecName;
    private long _bitRate;
    private double _frameRate;
    public SwsContext* _convert;
    public int_array4 _targetLineSize;
    public byte_ptrArray4 _targetData;

    public TimeSpan Duration { get; set; }
    public TimeSpan FrameDuration { get; set; }
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    
    public IntPtr BufferPtr { get; set; }
    
    public AVFrame* MainFrame { get; set; }
    public AVPacket* MainPacket { get; set; }
    
    public Action? OnFrameDecoded { get; set; }
    
    public Timer? DecodeTimer { get; private set; }
    public bool IsPlaying { get; private set; } = false;
    

    public VideoDecoder(ILogger<VideoDecoder>? logger = null) 
    {
        this._logger = logger;
    }

    public bool InitializeFromFile(string path)
    {
        _formatContext = ffmpeg.avformat_alloc_context();
        if (_formatContext == null)
        {
            _logger?.LogError("Failed to allocate format context");
            return false;
        }

        var tempContext = _formatContext;
        var error = ffmpeg.avformat_open_input(&tempContext, path, null, null);
        if (error < 0)
        {
            _logger?.LogError($"Failed to open input file: {error}");
            return false;
        }
        error = ffmpeg.avformat_find_stream_info(_formatContext, null);
        if (error < 0)
        {
            _logger?.LogError($"Failed to find stream info: {error}");
            return false;
        }
        AVCodec* codec = null;
        _videoStreamIndex = ffmpeg.av_find_best_stream(_formatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &codec, 0);
        if (_videoStreamIndex < 0)
        {
            _logger?.LogError("Failed to find video stream");
            return false;
        }
        _videoStream = _formatContext->streams[_videoStreamIndex];
        _codecContext = ffmpeg.avcodec_alloc_context3(codec);
        error = ffmpeg.avcodec_parameters_to_context(_codecContext, _videoStream->codecpar);
        if (error < 0)
        {
            _logger?.LogError($"Failed to copy codec parameters to codec context: {error}");
            return false;
        }
        Duration = TimeSpan.FromMilliseconds(_videoStream->duration * ffmpeg.av_q2d(_videoStream->time_base) * 1000);
        _codecId = _codecContext->codec_id;
        _codecName = ffmpeg.avcodec_get_name(_codecId);
        _bitRate = _codecContext->bit_rate;
        _frameRate = ffmpeg.av_q2d(_videoStream->r_frame_rate);
        FrameHeight = _codecContext->height;
        FrameWidth = _codecContext->width;
        FrameDuration = TimeSpan.FromMilliseconds(1000 / _frameRate);
        var couldInitConvert = InitConvert(FrameWidth, FrameHeight, _codecContext->pix_fmt, FrameWidth, FrameHeight, AVPixelFormat.AV_PIX_FMT_BGR0);
        if (!couldInitConvert)
        {
            return false;
        }
        
        MainPacket = ffmpeg.av_packet_alloc();
        MainFrame = ffmpeg.av_frame_alloc();
        if (MainFrame == null || MainPacket == null)
        {
            _logger?.LogError("Failed to allocate frame or packet");
            return false;
        }
        DecodeTimer = new Timer(FrameDuration);
        DecodeTimer.Elapsed += PushFrame;
        return true;
    }

    public Queue<AVFrame> Frames { get; } = new();
    private readonly object _frameReadLock = new();
    private void PushFrame(object? sender, ElapsedEventArgs e)
    {
        lock (_frameReadLock)
        {
            int result = -1;
            ffmpeg.av_frame_unref(MainFrame);
            while (true)
            {
                ffmpeg.av_packet_unref(MainPacket);
                result = ffmpeg.av_read_frame(_formatContext, MainPacket);
                if (result == ffmpeg.AVERROR_EOF || result < 0)
                {
                    var outFrame = *MainFrame;
                    Frames.Enqueue(outFrame);
                    OnFrameDecoded?.Invoke();
                    return;
                }
                if (MainPacket->stream_index != _videoStreamIndex)
                    continue;
                ffmpeg.avcodec_send_packet(_codecContext, MainPacket);
                result = ffmpeg.avcodec_receive_frame(_codecContext, MainFrame);
                if (result < 0) continue;
                var outFrame2 = *MainFrame;
                Frames.Enqueue(outFrame2);
                OnFrameDecoded?.Invoke();
                return;
            }
        }
    }
    
    public void FrameConvert(AVFrame* sourceFrame, ref byte_ptrArray8 existingData, ref int_array8 existingLineSize)
    {
        ffmpeg.sws_scale(_convert, sourceFrame->data, sourceFrame->linesize, 0, sourceFrame->height, existingData, existingLineSize);
        existingData.UpdateFrom(_targetData);
        existingLineSize.UpdateFrom(_targetLineSize);
    }
    
    private bool InitConvert(int sourceWidth, int sourceHeight, AVPixelFormat sourceFormat, int targetWidth, int targetHeight, AVPixelFormat targetFormat)
    {
        try
        {
            _convert = ffmpeg.sws_getContext(sourceWidth, sourceHeight, sourceFormat, targetWidth, targetHeight, targetFormat, ffmpeg.SWS_FAST_BILINEAR, null, null, null);
            if (_convert == null)
            {
                _logger?.LogError("Failed to initialize conversion context");
                return false;
            }
            var bufferSizes = ffmpeg.av_image_get_buffer_size(targetFormat, targetWidth, targetHeight, 1);
            BufferPtr = Marshal.AllocHGlobal(bufferSizes);
            _targetData = new byte_ptrArray4();
            _targetLineSize = new int_array4();
            var error = ffmpeg.av_image_fill_arrays(ref _targetData, ref _targetLineSize, (byte*)BufferPtr, AVPixelFormat.AV_PIX_FMT_BGR0, targetWidth, targetHeight, 1);
            if (error < 0)
            {
                _logger?.LogError($"Failed to fill image arrays: {error}");
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to initialize conversion context");
            return false;
        }
    }
    
    public void Start()
    {
        if (IsPlaying)
            return;
        DecodeTimer?.Start();
        IsPlaying = true;
    }
    
    public void Stop()
    {
        if (!IsPlaying)
            return;
        DecodeTimer?.Stop();
        IsPlaying = false;
    }
}