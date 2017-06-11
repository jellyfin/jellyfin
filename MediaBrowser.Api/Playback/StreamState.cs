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
using System.Linq;
using System.Threading;
using MediaBrowser.Controller.MediaEncoding;

namespace MediaBrowser.Api.Playback
{
    public class StreamState : EncodingJobInfo, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IMediaSourceManager _mediaSourceManager;

        public string RequestedUrl { get; set; }

        public StreamRequest Request
        {
            get { return (StreamRequest)BaseRequest; }
            set
            {
                BaseRequest = value;

                IsVideoRequest = VideoRequest != null;
            }
        }

        public TranscodingThrottler TranscodingThrottler { get; set; }

        public VideoStreamRequest VideoRequest
        {
            get { return Request as VideoStreamRequest; }
        }

        /// <summary>
        /// Gets or sets the log file stream.
        /// </summary>
        /// <value>The log file stream.</value>
        public Stream LogFileStream { get; set; }
        public IDirectStreamProvider DirectStreamProvider { get; set; }

        public string WaitForPath { get; set; }

        public bool IsOutputVideo
        {
            get { return Request is VideoStreamRequest; }
        }

        public int SegmentLength
        {
            get
            {
                if (Request.SegmentLength.HasValue)
                {
                    return Request.SegmentLength.Value;
                }

                if (string.Equals(OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    var userAgent = UserAgent ?? string.Empty;

                    if (userAgent.IndexOf("AppleTV", StringComparison.OrdinalIgnoreCase) != -1 ||
                        userAgent.IndexOf("cfnetwork", StringComparison.OrdinalIgnoreCase) != -1 ||
                        userAgent.IndexOf("ipad", StringComparison.OrdinalIgnoreCase) != -1 ||
                        userAgent.IndexOf("iphone", StringComparison.OrdinalIgnoreCase) != -1 ||
                        userAgent.IndexOf("ipod", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        if (IsSegmentedLiveStream)
                        {
                            return 6;
                        }

                        return 10;
                    }

                    if (IsSegmentedLiveStream)
                    {
                        return 3;
                    }
                    return 6;
                }

                return 3;
            }
        }

        public int MinSegments
        {
            get
            {
                if (Request.MinSegments.HasValue)
                {
                    return Request.MinSegments.Value;
                }

                return SegmentLength >= 10 ? 2 : 3;
            }
        }

        public int HlsListSize
        {
            get
            {
                return 0;
            }
        }

        public string UserAgent { get; set; }

        public StreamState(IMediaSourceManager mediaSourceManager, ILogger logger, TranscodingJobType transcodingType) 
            : base(logger, transcodingType)
        {
            _mediaSourceManager = mediaSourceManager;
            _logger = logger;
        }

        public string MimeType { get; set; }

        public bool EstimateContentLength { get; set; }
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

        public bool EnableDlnaHeaders { get; set; }

        public void Dispose()
        {
            DisposeTranscodingThrottler();
            DisposeLiveStream();
            DisposeLogStream();
            DisposeIsoMount();

            TranscodingJob = null;
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

        public string OutputFilePath { get; set; }

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

        public DeviceProfile DeviceProfile { get; set; }

        public TranscodingJob TranscodingJob;
        public override void ReportTranscodingProgress(TimeSpan? transcodingPosition, float? framerate, double? percentComplete, long? bytesTranscoded, int? bitRate)
        {
            ApiEntryPoint.Instance.ReportTranscodingProgress(TranscodingJob, this, transcodingPosition, framerate, percentComplete, bytesTranscoded, bitRate);
        }
    }
}
