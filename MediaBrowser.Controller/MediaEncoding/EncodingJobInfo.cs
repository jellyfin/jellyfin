using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.MediaEncoding
{
    // For now, a common base class until the API and MediaEncoding classes are unified
    public abstract class EncodingJobInfo
    {
        private readonly ILogger _logger;

        public MediaStream VideoStream { get; set; }
        public VideoType VideoType { get; set; }
        public Dictionary<string, string> RemoteHttpHeaders { get; set; }
        public string OutputVideoCodec { get; set; }
        public MediaProtocol InputProtocol { get; set; }
        public string MediaPath { get; set; }
        public bool IsInputVideo { get; set; }
        public IIsoMount IsoMount { get; set; }
        public List<string> PlayableStreamFileNames { get; set; }
        public string OutputAudioCodec { get; set; }
        public int? OutputVideoBitrate { get; set; }
        public MediaStream SubtitleStream { get; set; }
        public SubtitleDeliveryMethod SubtitleDeliveryMethod { get; set; }
        public List<string> SupportedSubtitleCodecs { get; set; }

        public int InternalSubtitleStreamOffset { get; set; }
        public MediaSourceInfo MediaSource { get; set; }
        public User User { get; set; }

        public long? RunTimeTicks { get; set; }

        public bool ReadInputAtNativeFramerate { get; set; }

        private List<TranscodeReason> _transcodeReasons = null;
        public List<TranscodeReason> TranscodeReasons
        {
            get
            {
                if (_transcodeReasons == null)
                {
                    _transcodeReasons = (BaseRequest.TranscodeReasons ?? string.Empty)
                        .Split(',')
                        .Where(i => !string.IsNullOrWhiteSpace(i))
                        .Select(v => (TranscodeReason)Enum.Parse(typeof(TranscodeReason), v, true))
                        .ToList();
                }

                return _transcodeReasons;
            }
        }

        public bool IgnoreInputDts
        {
            get
            {
                return MediaSource.IgnoreDts;
            }
        }

        public bool IgnoreInputIndex
        {
            get
            {
                return MediaSource.IgnoreIndex;
            }
        }

        public bool GenPtsInput
        {
            get
            {
                return MediaSource.GenPtsInput;
            }
        }

        public bool DiscardCorruptFramesInput
        {
            get
            {
                return false;
            }
        }

        public bool EnableFastSeekInput
        {
            get
            {
                return false;
            }
        }

        public bool GenPtsOutput
        {
            get
            {
                return false;
            }
        }

        public string OutputContainer { get; set; }

        public string OutputVideoSync
        {
            get
            {
                // For live tv + in progress recordings
                if (string.Equals(InputContainer, "mpegts", StringComparison.OrdinalIgnoreCase) || string.Equals(InputContainer, "ts", StringComparison.OrdinalIgnoreCase))
                {
                    if (!MediaSource.RunTimeTicks.HasValue)
                    {
                        return "cfr";
                    }
                }

                return "-1";
            }
        }

        public string AlbumCoverPath { get; set; }

        public string InputAudioSync { get; set; }
        public string InputVideoSync { get; set; }
        public TransportStreamTimestamp InputTimestamp { get; set; }

        public MediaStream AudioStream { get; set; }
        public List<string> SupportedAudioCodecs { get; set; }
        public List<string> SupportedVideoCodecs { get; set; }
        public string InputContainer { get; set; }
        public IsoType? IsoType { get; set; }

        public bool EnableMpegtsM2TsMode { get; set; }

        public BaseEncodingJobOptions BaseRequest { get; set; }

        public long? StartTimeTicks
        {
            get { return BaseRequest.StartTimeTicks; }
        }

        public bool CopyTimestamps
        {
            get { return BaseRequest.CopyTimestamps; }
        }

        public int? OutputAudioBitrate;
        public int? OutputAudioChannels;
        public bool DeInterlace { get; set; }
        public bool IsVideoRequest { get; set; }
        public TranscodingJobType TranscodingType { get; set; }

        public EncodingJobInfo(ILogger logger, TranscodingJobType jobType)
        {
            _logger = logger;
            TranscodingType = jobType;
            RemoteHttpHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            PlayableStreamFileNames = new List<string>();
            SupportedAudioCodecs = new List<string>();
            SupportedVideoCodecs = new List<string>();
            SupportedSubtitleCodecs = new List<string>();
        }

        public bool IsSegmentedLiveStream
        {
            get
            {
                return TranscodingType != TranscodingJobType.Progressive && !RunTimeTicks.HasValue;
            }
        }

        public bool EnableBreakOnNonKeyFrames(string videoCodec)
        {
            if (TranscodingType != TranscodingJobType.Progressive)
            {
                if (IsSegmentedLiveStream)
                {
                    return false;
                }

                return BaseRequest.BreakOnNonKeyFrames && string.Equals(videoCodec, "copy", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

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
                        BaseRequest.Width,
                        BaseRequest.Height,
                        BaseRequest.MaxWidth,
                        BaseRequest.MaxHeight);

                    return Convert.ToInt32(newSize.Width);
                }

                if (!IsVideoRequest)
                {
                    return null;
                }

                return BaseRequest.MaxWidth ?? BaseRequest.Width;
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
                        BaseRequest.Width,
                        BaseRequest.Height,
                        BaseRequest.MaxWidth,
                        BaseRequest.MaxHeight);

                    return Convert.ToInt32(newSize.Height);
                }

                if (!IsVideoRequest)
                {
                    return null;
                }

                return BaseRequest.MaxHeight ?? BaseRequest.Height;
            }
        }

        public int? OutputAudioSampleRate
        {
            get
            {
                if (BaseRequest.Static || string.Equals(OutputAudioCodec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    if (AudioStream != null)
                    {
                        return AudioStream.SampleRate;
                    }
                }

                else if (BaseRequest.AudioSampleRate.HasValue)
                {
                    // Don't exceed what the encoder supports
                    // Seeing issues of attempting to encode to 88200
                    return Math.Min(44100, BaseRequest.AudioSampleRate.Value);
                }

                return null;
            }
        }

        public int? OutputAudioBitDepth
        {
            get
            {
                if (BaseRequest.Static || string.Equals(OutputAudioCodec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    if (AudioStream != null)
                    {
                        return AudioStream.BitDepth;
                    }
                }

                //else if (BaseRequest.AudioSampleRate.HasValue)
                //{
                //    // Don't exceed what the encoder supports
                //    // Seeing issues of attempting to encode to 88200
                //    return Math.Min(44100, BaseRequest.AudioSampleRate.Value);
                //}

                return null;
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
                var request = BaseRequest;

                return !string.IsNullOrEmpty(request.Level) && !request.Static
                    ? double.Parse(request.Level, CultureInfo.InvariantCulture)
                    : stream == null ? null : stream.Level;
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
                return stream == null || !BaseRequest.Static ? null : stream.BitDepth;
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
                return stream == null || !BaseRequest.Static ? null : stream.RefFrames;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public float? TargetFramerate
        {
            get
            {
                var stream = VideoStream;
                var requestedFramerate = BaseRequest.MaxFramerate ?? BaseRequest.Framerate;

                return requestedFramerate.HasValue && !BaseRequest.Static
                    ? requestedFramerate
                    : stream == null ? null : stream.AverageFrameRate ?? stream.RealFrameRate;
            }
        }

        public TransportStreamTimestamp TargetTimestamp
        {
            get
            {
                var defaultValue = string.Equals(OutputContainer, "m2ts", StringComparison.OrdinalIgnoreCase) ?
                    TransportStreamTimestamp.Valid :
                    TransportStreamTimestamp.None;

                return !BaseRequest.Static
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
                return !BaseRequest.Static
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
                return !string.IsNullOrEmpty(BaseRequest.Profile) && !BaseRequest.Static
                    ? BaseRequest.Profile
                    : stream == null ? null : stream.Profile;
            }
        }

        public string TargetVideoCodecTag
        {
            get
            {
                var stream = VideoStream;
                return !BaseRequest.Static
                    ? null
                    : stream == null ? null : stream.CodecTag;
            }
        }

        public bool? IsTargetAnamorphic
        {
            get
            {
                if (BaseRequest.Static)
                {
                    return VideoStream == null ? null : VideoStream.IsAnamorphic;
                }

                return false;
            }
        }

        public bool? IsTargetInterlaced
        {
            get
            {
                if (BaseRequest.Static)
                {
                    return VideoStream == null ? (bool?)null : VideoStream.IsInterlaced;
                }

                if (DeInterlace)
                {
                    return false;
                }

                return VideoStream == null ? (bool?)null : VideoStream.IsInterlaced;
            }
        }

        public bool? IsTargetAVC
        {
            get
            {
                if (BaseRequest.Static)
                {
                    return VideoStream == null ? null : VideoStream.IsAVC;
                }

                return false;
            }
        }

        public int? TargetVideoStreamCount
        {
            get
            {
                if (BaseRequest.Static)
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
                if (BaseRequest.Static)
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

        protected void DisposeIsoMount()
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

        public abstract void ReportTranscodingProgress(TimeSpan? transcodingPosition, float? framerate, double? percentComplete, long? bytesTranscoded, int? bitRate);
    }

    /// <summary>
    /// Enum TranscodingJobType
    /// </summary>
    public enum TranscodingJobType
    {
        /// <summary>
        /// The progressive
        /// </summary>
        Progressive,
        /// <summary>
        /// The HLS
        /// </summary>
        Hls,
        /// <summary>
        /// The dash
        /// </summary>
        Dash
    }
}
