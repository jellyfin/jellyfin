using System;
using System.Diagnostics;
using System.Threading;
using MediaBrowser.Api.Playback;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class TranscodingJob.
    /// </summary>
    public class TranscodingJob
    {
        /// <summary>
        /// Gets or sets the play session identifier.
        /// </summary>
        /// <value>The play session identifier.</value>
        public string PlaySessionId { get; set; }

        /// <summary>
        /// Gets or sets the live stream identifier.
        /// </summary>
        /// <value>The live stream identifier.</value>
        public string LiveStreamId { get; set; }

        public bool IsLiveOutput { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public MediaSourceInfo MediaSource { get; set; }
        public string Path { get; set; }
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public TranscodingJobType Type { get; set; }
        /// <summary>
        /// Gets or sets the process.
        /// </summary>
        /// <value>The process.</value>
        public Process Process { get; set; }
        public ILogger Logger { get; private set; }
        /// <summary>
        /// Gets or sets the active request count.
        /// </summary>
        /// <value>The active request count.</value>
        public int ActiveRequestCount { get; set; }
        /// <summary>
        /// Gets or sets the kill timer.
        /// </summary>
        /// <value>The kill timer.</value>
        private Timer KillTimer { get; set; }

        public string DeviceId { get; set; }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public object ProcessLock = new object();

        public bool HasExited { get; set; }
        public bool IsUserPaused { get; set; }

        public string Id { get; set; }

        public float? Framerate { get; set; }
        public double? CompletionPercentage { get; set; }

        public long? BytesDownloaded { get; set; }
        public long? BytesTranscoded { get; set; }
        public int? BitRate { get; set; }

        public long? TranscodingPositionTicks { get; set; }
        public long? DownloadPositionTicks { get; set; }

        public TranscodingThrottler TranscodingThrottler { get; set; }

        private readonly object _timerLock = new object();

        public DateTime LastPingDate { get; set; }
        public int PingTimeout { get; set; }

        public TranscodingJob(ILogger logger)
        {
            Logger = logger;
        }

        public void StopKillTimer()
        {
            lock (_timerLock)
            {
                KillTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void DisposeKillTimer()
        {
            lock (_timerLock)
            {
                if (KillTimer != null)
                {
                    KillTimer.Dispose();
                    KillTimer = null;
                }
            }
        }

        public void StartKillTimer(Action<object> callback)
        {
            StartKillTimer(callback, PingTimeout);
        }

        public void StartKillTimer(Action<object> callback, int intervalMs)
        {
            if (HasExited)
            {
                return;
            }

            lock (_timerLock)
            {
                if (KillTimer == null)
                {
                    Logger.LogDebug("Starting kill timer at {0}ms. JobId {1} PlaySessionId {2}", intervalMs, Id, PlaySessionId);
                    KillTimer = new Timer(new TimerCallback(callback), this, intervalMs, Timeout.Infinite);
                }
                else
                {
                    Logger.LogDebug("Changing kill timer to {0}ms. JobId {1} PlaySessionId {2}", intervalMs, Id, PlaySessionId);
                    KillTimer.Change(intervalMs, Timeout.Infinite);
                }
            }
        }

        public void ChangeKillTimerIfStarted()
        {
            if (HasExited)
            {
                return;
            }

            lock (_timerLock)
            {
                if (KillTimer != null)
                {
                    var intervalMs = PingTimeout;

                    Logger.LogDebug("Changing kill timer to {0}ms. JobId {1} PlaySessionId {2}", intervalMs, Id, PlaySessionId);
                    KillTimer.Change(intervalMs, Timeout.Infinite);
                }
            }
        }
    }
}
