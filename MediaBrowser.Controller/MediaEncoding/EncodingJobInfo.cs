using System;
using System.Collections.Generic;
using System.Globalization;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;

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
                return false;
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
                // For live tv + recordings
                if (string.Equals(InputContainer, "mpegts", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(InputContainer, "ts", StringComparison.OrdinalIgnoreCase))
                {
                    return "cfr";
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
        public int? OutputAudioSampleRate;
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
