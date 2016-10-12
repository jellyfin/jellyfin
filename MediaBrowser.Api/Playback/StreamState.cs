using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace MediaBrowser.Api.Playback
{
    public class StreamState : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IMediaSourceManager _mediaSourceManager;

        public string RequestedUrl { get; set; }

        public StreamRequest Request { get; set; }
        public TranscodingThrottler TranscodingThrottler { get; set; }

        public VideoStreamRequest VideoRequest
        {
            get { return Request as VideoStreamRequest; }
        }

        public Dictionary<string, string> RemoteHttpHeaders { get; set; }

        /// <summary>
        /// Gets or sets the log file stream.
        /// </summary>
        /// <value>The log file stream.</value>
        public Stream LogFileStream { get; set; }
        public IDirectStreamProvider DirectStreamProvider { get; set; }

        public string InputContainer { get; set; }

        public MediaSourceInfo MediaSource { get; set; }

        public MediaStream AudioStream { get; set; }
        public MediaStream VideoStream { get; set; }
        public MediaStream SubtitleStream { get; set; }

        /// <summary>
        /// Gets or sets the iso mount.
        /// </summary>
        /// <value>The iso mount.</value>
        public IIsoMount IsoMount { get; set; }

        public string MediaPath { get; set; }
        public string WaitForPath { get; set; }

        public MediaProtocol InputProtocol { get; set; }

        public bool IsOutputVideo
        {
            get { return Request is VideoStreamRequest; }
        }
        public bool IsInputVideo { get; set; }

        public VideoType VideoType { get; set; }
        public IsoType? IsoType { get; set; }

        public List<string> PlayableStreamFileNames { get; set; }

        public int SegmentLength
        {
            get
            {
                if (string.Equals(OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    var userAgent = UserAgent ?? string.Empty;
                    if (userAgent.IndexOf("AppleTV", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        return 10;
                    }
                    if (userAgent.IndexOf("cfnetwork", StringComparison.OrdinalIgnoreCase) != -1 ||
                        userAgent.IndexOf("ipad", StringComparison.OrdinalIgnoreCase) != -1 ||
                        userAgent.IndexOf("iphone", StringComparison.OrdinalIgnoreCase) != -1 ||
                        userAgent.IndexOf("ipod", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        return 10;
                    }

                    if (!RunTimeTicks.HasValue)
                    {
                        return 3;
                    }
                    return 6;
                }

                if (!RunTimeTicks.HasValue)
                {
                    return 3;
                }
                return 3;
            }
        }

        public int HlsListSize
        {
            get
            {
                return 0;
            }
        }

        public long? RunTimeTicks;

        public long? InputBitrate { get; set; }
        public long? InputFileSize { get; set; }

        public string OutputAudioSync = "1";
        public string OutputVideoSync = "-1";

        public List<string> SupportedAudioCodecs { get; set; }
        public List<string> SupportedVideoCodecs { get; set; }
        public string UserAgent { get; set; }

        public StreamState(IMediaSourceManager mediaSourceManager, ILogger logger)
        {
            _mediaSourceManager = mediaSourceManager;
            _logger = logger;
            SupportedAudioCodecs = new List<string>();
            SupportedVideoCodecs = new List<string>();
            PlayableStreamFileNames = new List<string>();
            RemoteHttpHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string InputAudioSync { get; set; }
        public string InputVideoSync { get; set; }

        public bool DeInterlace { get; set; }

        public bool ReadInputAtNativeFramerate { get; set; }

        public TransportStreamTimestamp InputTimestamp { get; set; }

        public string MimeType { get; set; }

        public bool EstimateContentLength { get; set; }
        public bool EnableMpegtsM2TsMode { get; set; }
        public TranscodeSeekInfo TranscodeSeekInfo { get; set; }

        public long? EncodingDurationTicks { get; set; }

        public string GetMimeType(string outputPath)
        {
            if (!string.IsNullOrEmpty(MimeType))
            {
                return MimeType;
            }

            return MimeTypes.GetMimeType(outputPath);
        }

        public void Dispose()
        {
            DisposeTranscodingThrottler();
            DisposeLiveStream();
            DisposeLogStream();
            DisposeIsoMount();
        }

        private void DisposeLogStream()
        {
            if (LogFileStream != null)
            {
                try
                {
                    LogFileStream.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error disposing log stream", ex);
                }

                LogFileStream = null;
            }
        }

        private void DisposeTranscodingThrottler()
        {
            if (TranscodingThrottler != null)
            {
                try
                {
                    TranscodingThrottler.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error disposing TranscodingThrottler", ex);
                }

                TranscodingThrottler = null;
            }
        }

        private void DisposeIsoMount()
        {
            if (IsoMount != null)
            {
                try
                {
                    IsoMount.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error disposing iso mount", ex);
                }

                IsoMount = null;
            }
        }

        private async void DisposeLiveStream()
        {
            if (MediaSource.RequiresClosing && string.IsNullOrWhiteSpace(Request.LiveStreamId) && !string.IsNullOrWhiteSpace(MediaSource.LiveStreamId))
            {
                try
                {
                    await _mediaSourceManager.CloseLiveStream(MediaSource.LiveStreamId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error closing media source", ex);
                }
            }
        }

        public int InternalSubtitleStreamOffset { get; set; }

        public string OutputFilePath { get; set; }
        public string OutputVideoCodec { get; set; }
        public string OutputAudioCodec { get; set; }
        public int? OutputAudioChannels;
        public int? OutputAudioSampleRate;
        public int? OutputAudioBitrate;
        public int? OutputVideoBitrate;

        public string ActualOutputVideoCodec
        {
            get
            {
                var codec = OutputVideoCodec;

                if (string.Equals(codec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    var stream = VideoStream;

                    if (stream != null)
                    {
                        return stream.Codec;
                    }

                    return null;
                }

                return codec;
            }
        }

        public string ActualOutputAudioCodec
        {
            get
            {
                var codec = OutputAudioCodec;

                if (string.Equals(codec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    var stream = AudioStream;

                    if (stream != null)
                    {
                        return stream.Codec;
                    }

                    return null;
                }

                return codec;
            }
        }

        public string OutputContainer { get; set; }

        public DeviceProfile DeviceProfile { get; set; }

        public int? TotalOutputBitrate
        {
            get
            {
                return (OutputAudioBitrate ?? 0) + (OutputVideoBitrate ?? 0);
            }
        }

        public int? OutputWidth
        {
            get
            {
                if (VideoStream != null && VideoStream.Width.HasValue && VideoStream.Height.HasValue)
                {
                    var size = new ImageSize
                    {
                        Width = VideoStream.Width.Value,
                        Height = VideoStream.Height.Value
                    };

                    var newSize = DrawingUtils.Resize(size,
                        VideoRequest.Width,
                        VideoRequest.Height,
                        VideoRequest.MaxWidth,
                        VideoRequest.MaxHeight);

                    return Convert.ToInt32(newSize.Width);
                }

                if (VideoRequest == null)
                {
                    return null;
                }

                return VideoRequest.MaxWidth ?? VideoRequest.Width;
            }
        }

        public int? OutputHeight
        {
            get
            {
                if (VideoStream != null && VideoStream.Width.HasValue && VideoStream.Height.HasValue)
                {
                    var size = new ImageSize
                    {
                        Width = VideoStream.Width.Value,
                        Height = VideoStream.Height.Value
                    };

                    var newSize = DrawingUtils.Resize(size,
                        VideoRequest.Width,
                        VideoRequest.Height,
                        VideoRequest.MaxWidth,
                        VideoRequest.MaxHeight);

                    return Convert.ToInt32(newSize.Height);
                }

                if (VideoRequest == null)
                {
                    return null;
                }

                return VideoRequest.MaxHeight ?? VideoRequest.Height;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public int? TargetVideoBitDepth
        {
            get
            {
                var stream = VideoStream;
                return stream == null || !Request.Static ? null : stream.BitDepth;
            }
        }

        /// <summary>
        /// Gets the target reference frames.
        /// </summary>
        /// <value>The target reference frames.</value>
        public int? TargetRefFrames
        {
            get
            {
                var stream = VideoStream;
                return stream == null || !Request.Static ? null : stream.RefFrames;
            }
        }

        public int? TargetVideoStreamCount
        {
            get
            {
                if (Request.Static)
                {
                    return GetMediaStreamCount(MediaStreamType.Video, int.MaxValue);
                }
                return GetMediaStreamCount(MediaStreamType.Video, 1);
            }
        }

        public int? TargetAudioStreamCount
        {
            get
            {
                if (Request.Static)
                {
                    return GetMediaStreamCount(MediaStreamType.Audio, int.MaxValue);
                }
                return GetMediaStreamCount(MediaStreamType.Audio, 1);
            }
        }

        private int? GetMediaStreamCount(MediaStreamType type, int limit)
        {
            var count = MediaSource.GetStreamCount(type);

            if (count.HasValue)
            {
                count = Math.Min(count.Value, limit);
            }

            return count;
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public float? TargetFramerate
        {
            get
            {
                var stream = VideoStream;
                var requestedFramerate = VideoRequest.MaxFramerate ?? VideoRequest.Framerate;

                return requestedFramerate.HasValue && !Request.Static
                    ? requestedFramerate
                    : stream == null ? null : stream.AverageFrameRate ?? stream.RealFrameRate;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public double? TargetVideoLevel
        {
            get
            {
                var stream = VideoStream;
                return !string.IsNullOrEmpty(VideoRequest.Level) && !Request.Static
                    ? double.Parse(VideoRequest.Level, CultureInfo.InvariantCulture)
                    : stream == null ? null : stream.Level;
            }
        }

        public TransportStreamTimestamp TargetTimestamp
        {
            get
            {
                var defaultValue = string.Equals(OutputContainer, "m2ts", StringComparison.OrdinalIgnoreCase) ?
                    TransportStreamTimestamp.Valid :
                    TransportStreamTimestamp.None;

                return !Request.Static
                    ? defaultValue
                    : InputTimestamp;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public int? TargetPacketLength
        {
            get
            {
                var stream = VideoStream;
                return !Request.Static
                    ? null
                    : stream == null ? null : stream.PacketLength;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public string TargetVideoProfile
        {
            get
            {
                var stream = VideoStream;
                return !string.IsNullOrEmpty(VideoRequest.Profile) && !Request.Static
                    ? VideoRequest.Profile
                    : stream == null ? null : stream.Profile;
            }
        }

        public string TargetVideoCodecTag
        {
            get
            {
                var stream = VideoStream;
                return !Request.Static
                    ? null
                    : stream == null ? null : stream.CodecTag;
            }
        }

        public bool? IsTargetAnamorphic
        {
            get
            {
                if (Request.Static)
                {
                    return VideoStream == null ? null : VideoStream.IsAnamorphic;
                }

                return false;
            }
        }

        public bool? IsTargetAVC
        {
            get
            {
                if (Request.Static)
                {
                    return VideoStream == null ? null : VideoStream.IsAVC;
                }

                return true;
            }
        }
    }
}
