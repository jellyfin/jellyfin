using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Threading;

namespace MediaBrowser.Api.Playback
{
    public class TranscodingThrottler : IDisposable
    {
        private readonly TranscodingJob _job;
        private readonly ILogger _logger;
        private Timer _timer;
        private bool _isPaused;
        private readonly IConfigurationManager _config;

        public TranscodingThrottler(TranscodingJob job, ILogger logger, IConfigurationManager config)
        {
            _job = job;
            _logger = logger;
            _config = config;
        }

        private EncodingOptions GetOptions()
        {
            return _config.GetConfiguration<EncodingOptions>("encoding");
        }

        public void Start()
        {
            _timer = new Timer(TimerCallback, null, 5000, 5000);
        }

        private void TimerCallback(object state)
        {
            if (_job.HasExited)
            {
                DisposeTimer();
                return;
            }

            var options = GetOptions();

            if (options.EnableThrottling && IsThrottleAllowed(_job, options.ThrottleDelaySeconds))
            {
                PauseTranscoding();
            }
            else
            {
                UnpauseTranscoding();
            }
        }

        private void PauseTranscoding()
        {
            if (!_isPaused)
            {
                _logger.Debug("Sending pause command to ffmpeg");

                try
                {
                    _job.Process.StandardInput.Write("c");
                    _isPaused = true;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error pausing transcoding", ex);
                }
            }
        }

        public void UnpauseTranscoding()
        {
            if (_isPaused)
            {
                _logger.Debug("Sending unpause command to ffmpeg");

                try
                {
                    _job.Process.StandardInput.WriteLine();
                    _isPaused = false;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error unpausing transcoding", ex);
                }
            }
        }

        private bool IsThrottleAllowed(TranscodingJob job, int thresholdSeconds)
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
                    //_logger.Debug("Not throttling transcoder gap {0} target gap {1}", gap, targetGap);
                    return false;
                }

                //_logger.Debug("Throttling transcoder gap {0} target gap {1}", gap, targetGap);
                return true;
            }

            if (bytesDownloaded > 0 && transcodingPositionTicks > 0)
            {
                // Progressive Streaming - byte-based consideration

                try
                {
                    var bytesTranscoded = job.BytesTranscoded ?? new FileInfo(path).Length;

                    // Estimate the bytes the transcoder should be ahead
                    double gapFactor = gapLengthInTicks;
                    gapFactor /= transcodingPositionTicks;
                    var targetGap = bytesTranscoded * gapFactor;

                    var gap = bytesTranscoded - bytesDownloaded;

                    if (gap < targetGap)
                    {
                        //_logger.Debug("Not throttling transcoder gap {0} target gap {1} bytes downloaded {2}", gap, targetGap, bytesDownloaded);
                        return false;
                    }

                    //_logger.Debug("Throttling transcoder gap {0} target gap {1} bytes downloaded {2}", gap, targetGap, bytesDownloaded);
                    return true;
                }
                catch
                {
                    //_logger.Error("Error getting output size");
                    return false;
                }
            }

            //_logger.Debug("No throttle data for " + path);
            return false;
        }

        public void Stop()
        {
            DisposeTimer();
            UnpauseTranscoding();
        }

        public void Dispose()
        {
            DisposeTimer();
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
