using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

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
        public string[] PlayableStreamFileNames { get; set; }
        public string OutputAudioCodec { get; set; }
        public int? OutputVideoBitrate { get; set; }
        public MediaStream SubtitleStream { get; set; }
        public SubtitleDeliveryMethod SubtitleDeliveryMethod { get; set; }
        public string[] SupportedSubtitleCodecs { get; set; }

        public int InternalSubtitleStreamOffset { get; set; }
        public MediaSourceInfo MediaSource { get; set; }
        public User User { get; set; }

        public long? RunTimeTicks { get; set; }

        public bool ReadInputAtNativeFramerate { get; set; }

        private TranscodeReason[] _transcodeReasons = null;
        public TranscodeReason[] TranscodeReasons
        {
            get
            {
                if (_transcodeReasons == null)
                {
                    _transcodeReasons = (BaseRequest.TranscodeReasons ?? string.Empty)
                        .Split(',')
                        .Where(i => !string.IsNullOrEmpty(i))
                        .Select(v => (TranscodeReason)Enum.Parse(typeof(TranscodeReason), v, true))
                        .ToArray();
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
        public string[] SupportedAudioCodecs { get; set; }
        public string[] SupportedVideoCodecs { get; set; }
        public string InputContainer { get; set; }
        public IsoType? IsoType { get; set; }

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

        public bool DeInterlace(string videoCodec, bool forceDeinterlaceIfSourceIsInterlaced)
        {
            var videoStream = VideoStream;
            var isInputInterlaced = videoStream != null && videoStream.IsInterlaced;

            if (!isInputInterlaced)
            {
                return false;
            }

            // Support general param
            if (BaseRequest.DeInterlace)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(videoCodec))
            {
                if (string.Equals(BaseRequest.GetOption(videoCodec, "deinterlace"), "true", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (forceDeinterlaceIfSourceIsInterlaced)
            {
                if (isInputInterlaced)
                {
                    return true;
                }
            }

            return false;
        }

        public string[] GetRequestedProfiles(string codec)
        {
            if (!string.IsNullOrEmpty(BaseRequest.Profile))
            {
                return BaseRequest.Profile.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries);
            }

            if (!string.IsNullOrEmpty(codec))
            {
                var profile = BaseRequest.GetOption(codec, "profile");

                if (!string.IsNullOrEmpty(profile))
                {
                    return profile.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries);
                }
            }

            return Array.Empty<string>();
        }

        public string GetRequestedLevel(string codec)
        {
            if (!string.IsNullOrEmpty(BaseRequest.Level))
            {
                return BaseRequest.Level;
            }

            if (!string.IsNullOrEmpty(codec))
            {
                return BaseRequest.GetOption(codec, "level");
            }

            return null;
        }

        public int? GetRequestedMaxRefFrames(string codec)
        {
            if (BaseRequest.MaxRefFrames.HasValue)
            {
                return BaseRequest.MaxRefFrames;
            }

            if (!string.IsNullOrEmpty(codec))
            {
                var value = BaseRequest.GetOption(codec, "maxrefframes");
                int result;
                if (!string.IsNullOrEmpty(value) && int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }
            }

            return null;
        }

        public int? GetRequestedVideoBitDepth(string codec)
        {
            if (BaseRequest.MaxVideoBitDepth.HasValue)
            {
                return BaseRequest.MaxVideoBitDepth;
            }

            if (!string.IsNullOrEmpty(codec))
            {
                var value = BaseRequest.GetOption(codec, "videobitdepth");
                int result;
                if (!string.IsNullOrEmpty(value) && int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }
            }

            return null;
        }

        public int? GetRequestedAudioBitDepth(string codec)
        {
            if (BaseRequest.MaxAudioBitDepth.HasValue)
            {
                return BaseRequest.MaxAudioBitDepth;
            }

            if (!string.IsNullOrEmpty(codec))
            {
                var value = BaseRequest.GetOption(codec, "audiobitdepth");
                int result;
                if (!string.IsNullOrEmpty(value) && int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }
            }

            return null;
        }

        public int? GetRequestedAudioChannels(string codec)
        {
            if (BaseRequest.MaxAudioChannels.HasValue)
            {
                return BaseRequest.MaxAudioChannels;
            }
            if (BaseRequest.AudioChannels.HasValue)
            {
                return BaseRequest.AudioChannels;
            }

            if (!string.IsNullOrEmpty(codec))
            {
                var value = BaseRequest.GetOption(codec, "audiochannels");
                int result;
                if (!string.IsNullOrEmpty(value) && int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }
            }

            return null;
        }

        public bool IsVideoRequest { get; set; }
        public TranscodingJobType TranscodingType { get; set; }

        public EncodingJobInfo(ILogger logger, IMediaSourceManager unused, TranscodingJobType jobType)
        {
            _logger = logger;
            TranscodingType = jobType;
            RemoteHttpHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            PlayableStreamFileNames = Array.Empty<string>();
            SupportedAudioCodecs = Array.Empty<string>();
            SupportedVideoCodecs = Array.Empty<string>();
            SupportedSubtitleCodecs = Array.Empty<string>();
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
                        BaseRequest.Width ?? 0,
                        BaseRequest.Height ?? 0,
                        BaseRequest.MaxWidth ?? 0,
                        BaseRequest.MaxHeight ?? 0);

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
                        BaseRequest.Width ?? 0,
                        BaseRequest.Height ?? 0,
                        BaseRequest.MaxWidth ?? 0,
                        BaseRequest.MaxHeight ?? 0);

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
                if (BaseRequest.Static || string.Equals(OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    return VideoStream == null ? null : VideoStream.Level;
                }

                var level = GetRequestedLevel(ActualOutputVideoCodec);
                double result;
                if (!string.IsNullOrEmpty(level) && double.TryParse(level, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }

                return null;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public int? TargetVideoBitDepth
        {
            get
            {
                if (BaseRequest.Static || string.Equals(OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    return VideoStream == null ? null : VideoStream.BitDepth;
                }

                return null;
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
                if (BaseRequest.Static || string.Equals(OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    return VideoStream == null ? null : VideoStream.RefFrames;
                }

                return null;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public float? TargetFramerate
        {
            get
            {
                if (BaseRequest.Static || string.Equals(OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    return VideoStream == null ? null : (VideoStream.AverageFrameRate ?? VideoStream.RealFrameRate);
                }

                return BaseRequest.MaxFramerate ?? BaseRequest.Framerate;
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
                if (BaseRequest.Static || string.Equals(OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    return VideoStream == null ? null : VideoStream.PacketLength;
                }

                return null;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public string TargetVideoProfile
        {
            get
            {
                if (BaseRequest.Static || string.Equals(OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    return VideoStream == null ? null : VideoStream.Profile;
                }

                var requestedProfile = GetRequestedProfiles(ActualOutputVideoCodec).FirstOrDefault();
                if (!string.IsNullOrEmpty(requestedProfile))
                {
                    return requestedProfile;
                }

                return null;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public string TargetVideoRange
        {
            get
            {
                if (BaseRequest.Static || string.Equals(OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    return VideoStream == null ? null : VideoStream.VideoRange;
                }

                return "SDR";
            }
        }

        public string TargetAudioProfile
        {
            get
            {
                if (BaseRequest.Static || string.Equals(OutputAudioCodec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    return AudioStream == null ? null : AudioStream.Profile;
                }

                return null;
            }
        }

        public string TargetVideoCodecTag
        {
            get
            {
                if (BaseRequest.Static || string.Equals(OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    return VideoStream == null ? null : VideoStream.CodecTag;
                }

                return null;
            }
        }

        public bool? IsTargetAnamorphic
        {
            get
            {
                if (BaseRequest.Static || string.Equals(OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    return VideoStream == null ? null : VideoStream.IsAnamorphic;
                }

                return false;
            }
        }

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

        public bool? IsTargetInterlaced
        {
            get
            {
                if (BaseRequest.Static || string.Equals(OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    return VideoStream == null ? (bool?)null : VideoStream.IsInterlaced;
                }

                if (DeInterlace(ActualOutputVideoCodec, true))
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
                if (BaseRequest.Static || string.Equals(OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
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
                    _logger.LogError(ex, "Error disposing iso mount");
                }

                IsoMount = null;
            }
        }

        public IProgress<double> Progress { get; set; }
        public virtual void ReportTranscodingProgress(TimeSpan? transcodingPosition, float framerate, double? percentComplete, long bytesTranscoded, int? bitRate) {
            Progress.Report(percentComplete.Value);
        }

        public virtual void Dispose () {
        }
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
