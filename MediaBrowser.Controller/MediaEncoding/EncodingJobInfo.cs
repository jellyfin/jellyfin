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
    public class EncodingJobInfo
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

        public string OutputContainer { get; set; }

        public string OutputVideoSync = "-1";
        public string OutputAudioSync = "1";
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

        public EncodingJobInfo(ILogger logger)
        {
            _logger = logger;
            RemoteHttpHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            PlayableStreamFileNames = new List<string>();
            SupportedAudioCodecs = new List<string>();
            SupportedVideoCodecs = new List<string>();
            SupportedSubtitleCodecs = new List<string>();
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
    }
}
