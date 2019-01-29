using System;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class EncodingJob : EncodingJobInfo, IDisposable
    {
        public bool HasExited { get; internal set; }
        public bool IsCancelled { get; internal set; }

        public Stream LogFileStream { get; set; }
        public TaskCompletionSource<bool> TaskCompletionSource;

        public EncodingJobOptions Options
        {
            get => (EncodingJobOptions)BaseRequest;
            set => BaseRequest = value;
        }

        public Guid Id { get; set; }

        public bool EstimateContentLength { get; set; }
        public TranscodeSeekInfo TranscodeSeekInfo { get; set; }

        public string ItemType { get; set; }

        private readonly ILogger _logger;
        private readonly IMediaSourceManager _mediaSourceManager;

        public EncodingJob(ILogger logger, IMediaSourceManager mediaSourceManager) :
            base(TranscodingJobType.Progressive)
        {
            _logger = logger;
            _mediaSourceManager = mediaSourceManager;
            Id = Guid.NewGuid();

            TaskCompletionSource = new TaskCompletionSource<bool>();
        }

        public override void Dispose()
        {
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
                    _logger.LogError(ex, "Error disposing log stream");
                }

                LogFileStream = null;
            }
        }

        private async void DisposeLiveStream()
        {
            if (MediaSource.RequiresClosing && string.IsNullOrWhiteSpace(Options.LiveStreamId) && !string.IsNullOrWhiteSpace(MediaSource.LiveStreamId))
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
                    _logger.LogError("Error disposing iso mount", ex);
                }

                IsoMount = null;
            }
        }

        public void ReportTranscodingProgress(TimeSpan? transcodingPosition, float? framerate, double? percentComplete, long? bytesTranscoded, int? bitRate)
        {
            var ticks = transcodingPosition.HasValue ? transcodingPosition.Value.Ticks : (long?)null;

            //job.Framerate = framerate;

            if (!percentComplete.HasValue && ticks.HasValue && RunTimeTicks.HasValue)
            {
                var pct = ticks.Value / RunTimeTicks.Value;
                percentComplete = pct * 100;
            }

            if (percentComplete.HasValue)
            {
                Progress.Report(percentComplete.Value);
            }

            /*
            job.TranscodingPositionTicks = ticks;
            job.BytesTranscoded = bytesTranscoded;

            var deviceId = Options.DeviceId;

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                var audioCodec = ActualOutputVideoCodec;
                var videoCodec = ActualOutputVideoCodec;

                SessionManager.ReportTranscodingInfo(deviceId, new TranscodingInfo
                {
                    Bitrate = job.TotalOutputBitrate,
                    AudioCodec = audioCodec,
                    VideoCodec = videoCodec,
                    Container = job.Options.OutputContainer,
                    Framerate = framerate,
                    CompletionPercentage = percentComplete,
                    Width = job.OutputWidth,
                    Height = job.OutputHeight,
                    AudioChannels = job.OutputAudioChannels,
                    IsAudioDirect = string.Equals(job.OutputAudioCodec, "copy", StringComparison.OrdinalIgnoreCase),
                    IsVideoDirect = string.Equals(job.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase)
                });
            }*/
        }
    }
}
