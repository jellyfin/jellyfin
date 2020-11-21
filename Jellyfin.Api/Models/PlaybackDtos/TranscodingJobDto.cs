using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Models.PlaybackDtos
{
    /// <summary>
    /// Class TranscodingJob.
    /// </summary>
    public class TranscodingJobDto
    {
        /// <summary>
        /// The process lock.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1051:NoVisibleInstanceFields", MessageId = "ProcessLock", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "SA1401:PrivateField", MessageId = "ProcessLock", Justification = "Imported from ServiceStack")]
        public readonly object ProcessLock = new object();

        /// <summary>
        /// Timer lock.
        /// </summary>
        private readonly object _timerLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodingJobDto"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TranscodingJobDto}"/> interface.</param>
        public TranscodingJobDto(ILogger<TranscodingJobDto> logger)
        {
            Logger = logger;
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
        /// Gets or sets a value indicating whether is live output.
        /// </summary>
        public bool IsLiveOutput { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public MediaSourceInfo? MediaSource { get; set; }

        /// <summary>
        /// Gets or sets path.
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
        /// Gets logger.
        /// </summary>
        public ILogger<TranscodingJobDto> Logger { get; private set; }

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
        /// Gets or sets device id.
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets cancellation token source.
        /// </summary>
        public CancellationTokenSource? CancellationTokenSource { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether has exited.
        /// </summary>
        public bool HasExited { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is user paused.
        /// </summary>
        public bool IsUserPaused { get; set; }

        /// <summary>
        /// Gets or sets id.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets framerate.
        /// </summary>
        public float? Framerate { get; set; }

        /// <summary>
        /// Gets or sets completion percentage.
        /// </summary>
        public double? CompletionPercentage { get; set; }

        /// <summary>
        /// Gets or sets bytes downloaded.
        /// </summary>
        public long? BytesDownloaded { get; set; }

        /// <summary>
        /// Gets or sets bytes transcoded.
        /// </summary>
        public long? BytesTranscoded { get; set; }

        /// <summary>
        /// Gets or sets bit rate.
        /// </summary>
        public int? BitRate { get; set; }

        /// <summary>
        /// Gets or sets transcoding position ticks.
        /// </summary>
        public long? TranscodingPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets download position ticks.
        /// </summary>
        public long? DownloadPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets transcoding throttler.
        /// </summary>
        public TranscodingThrottler? TranscodingThrottler { get; set; }

        /// <summary>
        /// Gets or sets last ping date.
        /// </summary>
        public DateTime LastPingDate { get; set; }

        /// <summary>
        /// Gets or sets ping timeout.
        /// </summary>
        public int PingTimeout { get; set; }

        /// <summary>
        /// Stop kill timer.
        /// </summary>
        public void StopKillTimer()
        {
            lock (_timerLock)
            {
                KillTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Dispose kill timer.
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
        /// Start kill timer.
        /// </summary>
        /// <param name="callback">Callback action.</param>
        public void StartKillTimer(Action<object?> callback)
        {
            StartKillTimer(callback, PingTimeout);
        }

        /// <summary>
        /// Start kill timer.
        /// </summary>
        /// <param name="callback">Callback action.</param>
        /// <param name="intervalMs">Callback interval.</param>
        public void StartKillTimer(Action<object?> callback, int intervalMs)
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

        /// <summary>
        /// Change kill timer if started.
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

                    Logger.LogDebug("Changing kill timer to {0}ms. JobId {1} PlaySessionId {2}", intervalMs, Id, PlaySessionId);
                    KillTimer.Change(intervalMs, Timeout.Infinite);
                }
            }
        }
    }
}
