using System;
using System.Diagnostics;
using System.Threading;
using MediaBrowser.Model.Dto;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// Class TranscodingJob.
/// </summary>
public sealed class TranscodingJob : IDisposable
{
    private readonly ILogger<TranscodingJob> _logger;
    private readonly Lock _processLock = new();
    private readonly Lock _timerLock = new();

    private Timer? _killTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscodingJob"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{TranscodingJobDto}"/> interface.</param>
    public TranscodingJob(ILogger<TranscodingJob> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the play session identifier.
    /// </summary>
    public string? PlaySessionId { get; set; }

    /// <summary>
    /// Gets or sets the live stream identifier.
    /// </summary>
    public string? LiveStreamId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether is live output.
    /// </summary>
    public bool IsLiveOutput { get; set; }

    /// <summary>
    /// Gets or sets the path.
    /// </summary>
    public MediaSourceInfo? MediaSource { get; set; }

    /// <summary>
    /// Gets or sets path.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public TranscodingJobType Type { get; set; }

    /// <summary>
    /// Gets or sets the process.
    /// </summary>
    public Process? Process { get; set; }

    /// <summary>
    /// Gets or sets the active request count.
    /// </summary>
    public int ActiveRequestCount { get; set; }

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
    /// Gets or sets exit code.
    /// </summary>
    public int ExitCode { get; set; }

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
    public long BytesDownloaded { get; set; }

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
    /// Gets or sets transcoding segment cleaner.
    /// </summary>
    public TranscodingSegmentCleaner? TranscodingSegmentCleaner { get; set; }

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
            _killTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    /// <summary>
    /// Dispose kill timer.
    /// </summary>
    public void DisposeKillTimer()
    {
        lock (_timerLock)
        {
            if (_killTimer is not null)
            {
                _killTimer.Dispose();
                _killTimer = null;
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
            if (_killTimer is null)
            {
                _logger.LogDebug("Starting kill timer at {0}ms. JobId {1} PlaySessionId {2}", intervalMs, Id, PlaySessionId);
                _killTimer = new Timer(new TimerCallback(callback), this, intervalMs, Timeout.Infinite);
            }
            else
            {
                _logger.LogDebug("Changing kill timer to {0}ms. JobId {1} PlaySessionId {2}", intervalMs, Id, PlaySessionId);
                _killTimer.Change(intervalMs, Timeout.Infinite);
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
            if (_killTimer is not null)
            {
                var intervalMs = PingTimeout;

                _logger.LogDebug("Changing kill timer to {0}ms. JobId {1} PlaySessionId {2}", intervalMs, Id, PlaySessionId);
                _killTimer.Change(intervalMs, Timeout.Infinite);
            }
        }
    }

    /// <summary>
    /// Stops the transcoding job.
    /// </summary>
    public void Stop()
    {
        lock (_processLock)
        {
#pragma warning disable CA1849 // Can't await in lock block
            TranscodingThrottler?.Stop().GetAwaiter().GetResult();
            TranscodingSegmentCleaner?.Stop();

            var process = Process;

            if (!HasExited)
            {
                try
                {
                    _logger.LogInformation("Stopping ffmpeg process with q command for {Path}", Path);

                    process!.StandardInput.WriteLine("q");

                    // Need to wait because killing is asynchronous.
                    if (!process.WaitForExit(5000))
                    {
                        _logger.LogInformation("Killing FFmpeg process for {Path}", Path);
                        process.Kill();
                    }
                }
                catch (InvalidOperationException)
                {
                }
            }
#pragma warning restore CA1849
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Process?.Dispose();
        Process = null;
        _killTimer?.Dispose();
        _killTimer = null;
        CancellationTokenSource?.Dispose();
        CancellationTokenSource = null;
        TranscodingThrottler?.Dispose();
        TranscodingThrottler = null;
        TranscodingSegmentCleaner?.Dispose();
        TranscodingSegmentCleaner = null;
    }
}
