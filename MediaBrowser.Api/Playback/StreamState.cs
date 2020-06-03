using System;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Api.Playback
{
    public class StreamState : EncodingJobInfo, IDisposable
    {
        private readonly IMediaSourceManager _mediaSourceManager;
        private bool _disposed = false;

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

                if (EncodingHelper.IsCopyCodec(OutputVideoCodec))
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

        public bool EstimateContentLength { get; set; }

        public TranscodeSeekInfo TranscodeSeekInfo { get; set; }

        public bool EnableDlnaHeaders { get; set; }

        public DeviceProfile DeviceProfile { get; set; }

        public TranscodingJob TranscodingJob { get; set; }

        public StreamState(IMediaSourceManager mediaSourceManager, TranscodingJobType transcodingType)
            : base(transcodingType)
        {
            _mediaSourceManager = mediaSourceManager;
        }

        public override void ReportTranscodingProgress(TimeSpan? transcodingPosition, float? framerate, double? percentComplete, long? bytesTranscoded, int? bitRate)
        {
            ApiEntryPoint.Instance.ReportTranscodingProgress(TranscodingJob, this, transcodingPosition, framerate, percentComplete, bytesTranscoded, bitRate);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // REVIEW: Is this the right place for this?
                if (MediaSource.RequiresClosing
                    && string.IsNullOrWhiteSpace(Request.LiveStreamId)
                    && !string.IsNullOrWhiteSpace(MediaSource.LiveStreamId))
                {
                    _mediaSourceManager.CloseLiveStream(MediaSource.LiveStreamId).GetAwaiter().GetResult();
                }

                TranscodingThrottler?.Dispose();
            }

            TranscodingThrottler = null;
            TranscodingJob = null;

            _disposed = true;
        }
    }
}
