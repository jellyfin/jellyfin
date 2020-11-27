using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Models.PlaybackDtos
{
    /// <summary>
    /// Transcoding throttler.
    /// </summary>
    public class TranscodingThrottler : IDisposable
    {
        private readonly TranscodingJobDto _job;
        private readonly ILogger<TranscodingThrottler> _logger;
        private readonly IConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private Timer? _timer;
        private bool _isPaused;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodingThrottler"/> class.
        /// </summary>
        /// <param name="job">Transcoding job dto.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{TranscodingThrottler}"/> interface.</param>
        /// <param name="config">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        public TranscodingThrottler(TranscodingJobDto job, ILogger<TranscodingThrottler> logger, IConfigurationManager config, IFileSystem fileSystem)
        {
            _job = job;
            _logger = logger;
            _config = config;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Start timer.
        /// </summary>
        public void Start()
        {
            _timer = new Timer(TimerCallback, null, 5000, 5000);
        }

        /// <summary>
        /// Unpause transcoding.
        /// </summary>
        /// <returns>A <see cref="Task"/>.</returns>
        public async Task UnpauseTranscoding()
        {
            if (_isPaused)
            {
                _logger.LogDebug("Sending resume command to ffmpeg");

                try
                {
                    await _job.Process!.StandardInput.WriteLineAsync().ConfigureAwait(false);
                    _isPaused = false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resuming transcoding");
                }
            }
        }

        /// <summary>
        /// Stop throttler.
        /// </summary>
        /// <returns>A <see cref="Task"/>.</returns>
        public async Task Stop()
        {
            DisposeTimer();
            await UnpauseTranscoding().ConfigureAwait(false);
        }

        /// <summary>
        /// Dispose throttler.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose throttler.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeTimer();
            }
        }

        private EncodingOptions GetOptions()
        {
            return _config.GetConfiguration<EncodingOptions>("encoding");
        }

        private async void TimerCallback(object? state)
        {
            if (_job.HasExited)
            {
                DisposeTimer();
                return;
            }

            var options = GetOptions();

            if (options.EnableThrottling && IsThrottleAllowed(_job, options.ThrottleDelaySeconds))
            {
                await PauseTranscoding().ConfigureAwait(false);
            }
            else
            {
                await UnpauseTranscoding().ConfigureAwait(false);
            }
        }

        private async Task PauseTranscoding()
        {
            if (!_isPaused)
            {
                _logger.LogDebug("Sending pause command to ffmpeg");

                try
                {
                    await _job.Process!.StandardInput.WriteAsync("c").ConfigureAwait(false);
                    _isPaused = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error pausing transcoding");
                }
            }
        }

        private bool IsThrottleAllowed(TranscodingJobDto job, int thresholdSeconds)
        {
            var bytesDownloaded = job.BytesDownloaded ?? 0;
            var transcodingPositionTicks = job.TranscodingPositionTicks ?? 0;
            var downloadPositionTicks = job.DownloadPositionTicks ?? 0;

            var path = job.Path;
            var gapLengthInTicks = TimeSpan.FromSeconds(thresholdSeconds).Ticks;

            if (downloadPositionTicks > 0 && transcodingPositionTicks > 0)
            {
                // HLS - time-based consideration

                var targetGap = gapLengthInTicks;
                var gap = transcodingPositionTicks - downloadPositionTicks;

                if (gap < targetGap)
                {
                    _logger.LogDebug("Not throttling transcoder gap {0} target gap {1}", gap, targetGap);
                    return false;
                }

                _logger.LogDebug("Throttling transcoder gap {0} target gap {1}", gap, targetGap);
                return true;
            }

            if (bytesDownloaded > 0 && transcodingPositionTicks > 0)
            {
                // Progressive Streaming - byte-based consideration

                try
                {
                    var bytesTranscoded = job.BytesTranscoded ?? _fileSystem.GetFileInfo(path).Length;

                    // Estimate the bytes the transcoder should be ahead
                    double gapFactor = gapLengthInTicks;
                    gapFactor /= transcodingPositionTicks;
                    var targetGap = bytesTranscoded * gapFactor;

                    var gap = bytesTranscoded - bytesDownloaded;

                    if (gap < targetGap)
                    {
                        _logger.LogDebug("Not throttling transcoder gap {0} target gap {1} bytes downloaded {2}", gap, targetGap, bytesDownloaded);
                        return false;
                    }

                    _logger.LogDebug("Throttling transcoder gap {0} target gap {1} bytes downloaded {2}", gap, targetGap, bytesDownloaded);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting output size");
                    return false;
                }
            }

            _logger.LogDebug("No throttle data for " + path);
            return false;
        }

        private void DisposeTimer()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
