using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class EncodingJob : EncodingJobInfo, IDisposable
    {
        public bool HasExited { get; internal set; }
        public bool IsCancelled { get; internal set; }

        public Stream LogFileStream { get; set; }
        public IProgress<double> Progress { get; set; }
        public TaskCompletionSource<bool> TaskCompletionSource;

        public EncodingJobOptions Options
        {
            get { return (EncodingJobOptions) BaseRequest; }
            set { BaseRequest = value; }
        }

        public string Id { get; set; }

        public string MimeType { get; set; }
        public bool EstimateContentLength { get; set; }
        public TranscodeSeekInfo TranscodeSeekInfo { get; set; }
        public long? EncodingDurationTicks { get; set; }

        public string ItemType { get; set; }

        public string GetMimeType(string outputPath)
        {
            if (!string.IsNullOrEmpty(MimeType))
            {
                return MimeType;
            }

            return MimeTypes.GetMimeType(outputPath);
        }

        private readonly ILogger _logger;
        private readonly IMediaSourceManager _mediaSourceManager;

        public EncodingJob(ILogger logger, IMediaSourceManager mediaSourceManager) : 
            base(logger, TranscodingJobType.Progressive)
        {
            _logger = logger;
            _mediaSourceManager = mediaSourceManager;
            Id = Guid.NewGuid().ToString("N");

            _logger = logger;
            TaskCompletionSource = new TaskCompletionSource<bool>();
        }

        public void Dispose()
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
                    _logger.ErrorException("Error disposing log stream", ex);
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
                        Options.Width,
                        Options.Height,
                        Options.MaxWidth,
                        Options.MaxHeight);

                    return Convert.ToInt32(newSize.Width);
                }

                if (!IsVideoRequest)
                {
                    return null;
                }

                return Options.MaxWidth ?? Options.Width;
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
                        Options.Width,
                        Options.Height,
                        Options.MaxWidth,
                        Options.MaxHeight);

                    return Convert.ToInt32(newSize.Height);
                }

                if (!IsVideoRequest)
                {
                    return null;
                }

                return Options.MaxHeight ?? Options.Height;
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
                return stream == null || !Options.Static ? null : stream.BitDepth;
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
                return stream == null || !Options.Static ? null : stream.RefFrames;
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
                var requestedFramerate = Options.MaxFramerate ?? Options.Framerate;

                return requestedFramerate.HasValue && !Options.Static
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

                return !Options.Static
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
                return !Options.Static
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
                return !string.IsNullOrEmpty(Options.Profile) && !Options.Static
                    ? Options.Profile
                    : stream == null ? null : stream.Profile;
            }
        }

        public string TargetVideoCodecTag
        {
            get
            {
                var stream = VideoStream;
                return !Options.Static
                    ? null
                    : stream == null ? null : stream.CodecTag;
            }
        }

        public bool? IsTargetAnamorphic
        {
            get
            {
                if (Options.Static)
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
                if (Options.Static)
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
                if (Options.Static)
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
                if (Options.Static)
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

        public override void ReportTranscodingProgress(TimeSpan? transcodingPosition, float? framerate, double? percentComplete, long? bytesTranscoded, int? bitRate)
        {
            var ticks = transcodingPosition.HasValue ? transcodingPosition.Value.Ticks : (long?)null;

            //    job.Framerate = framerate;

            if (!percentComplete.HasValue && ticks.HasValue && RunTimeTicks.HasValue)
            {
                var pct = ticks.Value / RunTimeTicks.Value;
                percentComplete = pct * 100;
            }

            if (percentComplete.HasValue)
            {
                Progress.Report(percentComplete.Value);
            }

            //    job.TranscodingPositionTicks = ticks;
            //    job.BytesTranscoded = bytesTranscoded;

            var deviceId = Options.DeviceId;

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                var audioCodec = ActualOutputVideoCodec;
                var videoCodec = ActualOutputVideoCodec;

                //    SessionManager.ReportTranscodingInfo(deviceId, new TranscodingInfo
                //    {
                //        Bitrate = job.TotalOutputBitrate,
                //        AudioCodec = audioCodec,
                //        VideoCodec = videoCodec,
                //        Container = job.Options.OutputContainer,
                //        Framerate = framerate,
                //        CompletionPercentage = percentComplete,
                //        Width = job.OutputWidth,
                //        Height = job.OutputHeight,
                //        AudioChannels = job.OutputAudioChannels,
                //        IsAudioDirect = string.Equals(job.OutputAudioCodec, "copy", StringComparison.OrdinalIgnoreCase),
                //        IsVideoDirect = string.Equals(job.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase)
                //    });
            }
        }
    }
}
