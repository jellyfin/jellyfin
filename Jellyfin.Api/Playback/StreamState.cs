using System;
using System.IO;
using Jellyfin.Controller.Library;
using Jellyfin.Controller.MediaEncoding;
using Jellyfin.Model.Dlna;
using Jellyfin.Model.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Playback
{
    public class StreamState : EncodingJobInfo, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IMediaSourceManager _mediaSourceManager;

        public string RequestedUrl { get; set; }

        public StreamRequest Request
        {
            get => (StreamRequest)BaseRequest;
            set
            {
                BaseRequest = value;

                IsVideoRequest = VideoRequest != null;
            }
        }

        public TranscodingThrottler TranscodingThrottler { get; set; }

        public VideoStreamRequest VideoRequest => Request as VideoStreamRequest;

        /// <summary>
        /// Gets or sets the log file stream.
        /// </summary>
        /// <value>The log file stream.</value>
        public Stream LogFileStream { get; set; }
        public IDirectStreamProvider DirectStreamProvider { get; set; }

        public string WaitForPath { get; set; }

        public bool IsOutputVideo => Request is VideoStreamRequest;

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

                        return 6;
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

        public string UserAgent { get; set; }

        public StreamState(IMediaSourceManager mediaSourceManager, ILogger logger, TranscodingJobType transcodingType)
            : base(transcodingType)
        {
            _mediaSourceManager = mediaSourceManager;
            _logger = logger;
        }

        public bool EstimateContentLength { get; set; }
        public TranscodeSeekInfo TranscodeSeekInfo { get; set; }

        public bool EnableDlnaHeaders { get; set; }

        public override void Dispose()
        {
            DisposeTranscodingThrottler();
            DisposeLogStream();
            DisposeLiveStream();

            TranscodingJob = null;
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
                    _logger.LogError(ex, "Error disposing TranscodingThrottler");
                }

                TranscodingThrottler = null;
            }
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
                    _logger.LogError(ex, "Error disposing log stream");
                }

                LogFileStream = null;
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
                    _logger.LogError(ex, "Error closing media source");
                }
            }
        }

        public DeviceProfile DeviceProfile { get; set; }

        public TranscodingJob TranscodingJob;
        public override void ReportTranscodingProgress(TimeSpan? transcodingPosition, float framerate, double? percentComplete, long bytesTranscoded, int? bitRate)
        {
            ApiEntryPoint.Instance.ReportTranscodingProgress(TranscodingJob, this, transcodingPosition, framerate, percentComplete, bytesTranscoded, bitRate);
        }
    }
}
