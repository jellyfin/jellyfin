using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;

namespace Jellyfin.LiveTv.Recordings;

/// <summary>
/// A recording that <see cref="RecordingsManager"/> is running: the public <see cref="ActiveRecordingInfo"/>
/// together with the state used to end it on schedule.
/// </summary>
internal sealed class ActiveRecording
{
    private readonly Lock _sync = new();
    private TaskCompletionSource _timerChanged = CreateTimerChangedSignal();
    private DateTime _endDate;
    private int _postPaddingSeconds;
    private bool _started;
    private bool _cancelledBeforeStart;

    public ActiveRecording(ActiveRecordingInfo info, TimerInfo timer)
    {
        Info = info;
        SetTimer(timer);
    }

    public ActiveRecordingInfo Info { get; }

    /// <summary>
    /// Gets or sets the output file chosen for this recording. It is set before the recorder starts,
    /// while <see cref="ActiveRecordingInfo.Path"/> stays empty until the file exists.
    /// </summary>
    public string? ReservedPath { get; set; }

    public bool IsInProgress
    {
        get
        {
            lock (_sync)
            {
                return _started;
            }
        }
    }

    private DateTime ScheduledEnd => _endDate.AddSeconds(_postPaddingSeconds);

    /// <summary>
    /// Applies a changed timer and wakes anything waiting on the scheduled end.
    /// </summary>
    /// <param name="timer">The changed timer.</param>
    /// <returns>The scheduled end, including post-padding, before and after the change.</returns>
    public (DateTime PreviousEnd, DateTime NewEnd) UpdateTimer(TimerInfo timer)
    {
        lock (_sync)
        {
            var previousEnd = ScheduledEnd;
            SetTimer(timer);

            if (!_started && timer.Status == RecordingStatus.Cancelled)
            {
                _cancelledBeforeStart = true;
            }

            SignalTimerChanged();
            return (previousEnd, ScheduledEnd);
        }
    }

    /// <summary>
    /// Marks the recording as started, unless it was cancelled first or has already started. The timer is
    /// applied, so anything waiting on the scheduled end is woken.
    /// </summary>
    /// <param name="timer">The current timer, which may have changed while the recording was starting.</param>
    /// <returns><c>true</c> if the caller should treat the recording as started.</returns>
    public bool TryMarkStarted(TimerInfo timer)
    {
        lock (_sync)
        {
            if (_started
                || _cancelledBeforeStart
                || timer.Status == RecordingStatus.Cancelled
                || Info.CancellationTokenSource.IsCancellationRequested)
            {
                return false;
            }

            SetTimer(timer);
            SignalTimerChanged();
            _started = true;
            return true;
        }
    }

    /// <summary>
    /// Gets the time left until the scheduled end.
    /// </summary>
    /// <param name="utcNow">The current time.</param>
    /// <param name="timerChanged">A task that completes the next time the timer changes.</param>
    /// <returns>The time left, which is zero or negative once the end has passed.</returns>
    public TimeSpan GetTimeRemaining(DateTime utcNow, out Task timerChanged)
    {
        lock (_sync)
        {
            timerChanged = _timerChanged.Task;
            return ScheduledEnd - utcNow;
        }
    }

    private static TaskCompletionSource CreateTimerChangedSignal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private void SetTimer(TimerInfo timer)
    {
        Info.Timer = timer;
        _endDate = timer.EndDate;

        _postPaddingSeconds = Math.Max(0, timer.PostPaddingSeconds);
    }

    private void SignalTimerChanged()
    {
        var signal = _timerChanged;
        _timerChanged = CreateTimerChangedSignal();
        signal.TrySetResult();
    }
}
