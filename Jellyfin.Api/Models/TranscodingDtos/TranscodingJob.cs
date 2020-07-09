using System;
using System.Diagnostics;
using System.Threading;
using Jellyfin.Api.Models.PlaybackDtos;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Models.TranscodingDtos
{
    /// <summary>
    /// The transcoding job.
    /// </summary>
    public class TranscodingJob
    {
        private readonly ILogger _logger;
        private readonly object _timerLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodingJob"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TranscodingJob}"/> interface.</param>
        public TranscodingJob(ILogger<TranscodingJob> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets or sets the play session identifier.
        /// </summary>
        /// <value>The play session identifier.</value>
        public string? PlaySessionId { get; set; }

        /// <summary>
        /// Gets or sets the live stream identifier.
        /// </summary>
        /// <value>The live stream identifier.</value>
        public string? LiveStreamId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the transcoding job is a live output.
        /// </summary>
        public bool IsLiveOutput { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public MediaSourceInfo? MediaSource { get; set; }

        /// <summary>
        /// Gets or sets the transcoding path.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public TranscodingJobType Type { get; set; }

        /// <summary>
        /// Gets or sets the process.
        /// </summary>
        /// <value>The process.</value>
        public Process? Process { get; set; }

        /// <summary>
        /// Gets or sets the active request count.
        /// </summary>
        /// <value>The active request count.</value>
        public int ActiveRequestCount { get; set; }

        /// <summary>
        /// Gets or sets the kill timer.
        /// </summary>
        /// <value>The kill timer.</value>
        private Timer? KillTimer { get; set; }

        /// <summary>
        /// Gets or sets the device id.
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the cancellation token source.
        /// </summary>
        public CancellationTokenSource? CancellationTokenSource { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the transcoding job has exited.
        /// </summary>
        public bool HasExited { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user has paused the video.
        /// </summary>
        public bool IsUserPaused { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the framerate.
        /// </summary>
        public float? Framerate { get; set; }

        /// <summary>
        /// Gets or sets the completion percentage.
        /// </summary>
        public double? CompletionPercentage { get; set; }

        /// <summary>
        /// Gets or sets the bytes downloaded.
        /// </summary>
        public long? BytesDownloaded { get; set; }

        /// <summary>
        /// Gets or sets the bytes transcoded.
        /// </summary>
        public long? BytesTranscoded { get; set; }

        /// <summary>
        /// Gets or sets the bitrate.
        /// </summary>
        public int? BitRate { get; set; }

        /// <summary>
        /// Gets or sets the transcoding position ticks.
        /// </summary>
        public long? TranscodingPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the download position ticks.
        /// </summary>
        public long? DownloadPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the transcodign throttler.
        /// </summary>
        public TranscodingThrottler? TranscodingThrottler { get; set; }

        /// <summary>
        /// Gets or sets the last ping datetime.
        /// </summary>
        public DateTime LastPingDate { get; set; }

        /// <summary>
        /// Gets or sets the ping timeout.
        /// </summary>
        public int PingTimeout { get; set; }

        /// <summary>
        /// Stops the kill timer.
        /// </summary>
        public void StopKillTimer()
        {
            lock (_timerLock)
            {
                KillTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Disposes the kill timer.
        /// </summary>
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

        /// <summary>
        /// Starts the kill timer.
        /// </summary>
        /// <param name="callback">The amount of ms the timer should wait before the transcoding job gets killed.</param>
        public void StartKillTimer(Action<object> callback)
        {
            StartKillTimer(callback, PingTimeout);
        }

        /// <summary>
        /// Starts the kill timer.
        /// </summary>
        /// <param name="callback">The <see cref="Action"/> to run when the kill timer has finished.</param>
        /// <param name="intervalMs">The amount of ms the timer should wait before the transcoding job gets killed.</param>
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
                    _logger.LogDebug($"Starting kill timer at {intervalMs}ms. JobId {Id} PlaySessionId {PlaySessionId}");
                    KillTimer = new Timer(new TimerCallback(callback), this, intervalMs, Timeout.Infinite);
                }
                else
                {
                    _logger.LogDebug($"Changing kill timer to {intervalMs}ms. JobId {Id} PlaySessionId {PlaySessionId}");
                    KillTimer.Change(intervalMs, Timeout.Infinite);
                }
            }
        }

        /// <summary>
        /// Changes the kill timer if it has started.
        /// </summary>
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

                    _logger.LogDebug($"Changing kill timer to {intervalMs}ms. JobId {Id} PlaySessionId {PlaySessionId}");
                    KillTimer.Change(intervalMs, Timeout.Infinite);
                }
            }
        }
    }
}
