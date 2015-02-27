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

        public void Start()
        {
            _timer = new Timer(TimerCallback, null, 1000, 1000);
        }

        private void TimerCallback(object state)
        {
            if (IsThrottleAllowed(_job))
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
            _logger.Debug("Sending pause command to ffmpeg");
            _job.Process.StandardInput.WriteLine("p");
        }

        private void UnpauseTranscoding()
        {
            _logger.Debug("Sending unpause command to ffmpeg");
            _job.Process.StandardInput.WriteLine("u");
        }

        private readonly long _gapLengthInTicks = TimeSpan.FromMinutes(2).Ticks;

        public TranscodingThrottler(TranscodingJob job, ILogger logger)
        {
            _job = job;
            _logger = logger;
        }

        private bool IsThrottleAllowed(TranscodingJob job)
        {
            //var job = string.IsNullOrEmpty(request.TranscodingJobId) ?
            //null :
            //ApiEntryPoint.Instance.GetTranscodingJob(request.TranscodingJobId);

            //var limits = new List<long>();
            //if (state.InputBitrate.HasValue)
            //{
            //    // Bytes per second
            //    limits.Add((state.InputBitrate.Value / 8));
            //}
            //if (state.InputFileSize.HasValue && state.RunTimeTicks.HasValue)
            //{
            //    var totalSeconds = TimeSpan.FromTicks(state.RunTimeTicks.Value).TotalSeconds;

            //    if (totalSeconds > 1)
            //    {
            //        var timeBasedLimit = state.InputFileSize.Value / totalSeconds;
            //        limits.Add(Convert.ToInt64(timeBasedLimit));
            //    }
            //}

            //// Take the greater of the above to methods, just to be safe
            //var throttleLimit = limits.Count > 0 ? limits.First() : 0;

            //// Pad to play it safe
            //var bytesPerSecond = Convert.ToInt64(1.05 * throttleLimit);

            //// Don't even start evaluating this until at least two minutes have content have been consumed
            //var targetGap = throttleLimit * 120;

            var bytesDownloaded = job.BytesDownloaded ?? 0;
            var transcodingPositionTicks = job.TranscodingPositionTicks ?? 0;
            var downloadPositionTicks = job.DownloadPositionTicks ?? 0;

            var path = job.Path;

            if (downloadPositionTicks > 0 && transcodingPositionTicks > 0)
            {
                // HLS - time-based consideration

                var targetGap = _gapLengthInTicks;
                var gap = transcodingPositionTicks - downloadPositionTicks;

                if (gap < targetGap)
                {
                    //Logger.Debug("Not throttling transcoder gap {0} target gap {1}", gap, targetGap);
                    return false;
                }

                //Logger.Debug("Throttling transcoder gap {0} target gap {1}", gap, targetGap);
                return true;
            }

            if (bytesDownloaded > 0 && transcodingPositionTicks > 0)
            {
                // Progressive Streaming - byte-based consideration

                try
                {
                    var bytesTranscoded = job.BytesTranscoded ?? new FileInfo(path).Length;

                    // Estimate the bytes the transcoder should be ahead
                    double gapFactor = _gapLengthInTicks;
                    gapFactor /= transcodingPositionTicks;
                    var targetGap = bytesTranscoded * gapFactor;

                    var gap = bytesTranscoded - bytesDownloaded;

                    if (gap < targetGap)
                    {
                        //Logger.Debug("Not throttling transcoder gap {0} target gap {1} bytes downloaded {2}", gap, targetGap, bytesDownloaded);
                        return false;
                    }

                    //Logger.Debug("Throttling transcoder gap {0} target gap {1} bytes downloaded {2}", gap, targetGap, bytesDownloaded);
                    return true;
                }
                catch
                {
                    //Logger.Error("Error getting output size");
                }
            }
            else
            {
                //Logger.Debug("No throttle data for " + path);
            }

            return false;
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
