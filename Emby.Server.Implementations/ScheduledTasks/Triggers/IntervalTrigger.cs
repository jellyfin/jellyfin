using System;
using System.Linq;
using System.Threading;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Triggers
{
    /// <summary>
    /// Represents a task trigger that runs repeatedly on an interval.
    /// </summary>
    public sealed class IntervalTrigger : ITaskTrigger, IDisposable
    {
        private readonly TimeSpan _interval;
        private DateTime _lastStartDate;
        private Timer? _timer;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntervalTrigger"/> class.
        /// </summary>
        /// <param name="interval">The interval.</param>
        /// <param name="taskOptions">The options of this task.</param>
        public IntervalTrigger(TimeSpan interval, TaskOptions taskOptions)
        {
            _interval = interval;
            TaskOptions = taskOptions;
        }

        /// <inheritdoc />
        public event EventHandler<EventArgs>? Triggered;

        /// <inheritdoc />
        public TaskOptions TaskOptions { get; }

        /// <inheritdoc />
        public void Start(TaskResult? lastResult, ILogger logger, string taskName, bool isApplicationStartup)
        {
            DisposeTimer();

            DateTime now = DateTime.UtcNow;
            DateTime triggerDate;

            if (lastResult is null)
            {
                // Task has never been completed before
                triggerDate = now.AddHours(1);
            }
            else
            {
                triggerDate = new[] { lastResult.EndTimeUtc, _lastStartDate, now.AddMinutes(1) }.Max().Add(_interval);
            }

            var dueTime = triggerDate - now;
            var maxDueTime = TimeSpan.FromDays(7);

            if (dueTime > maxDueTime)
            {
                dueTime = maxDueTime;
            }

            _timer = new Timer(_ => OnTriggered(), null, dueTime, TimeSpan.FromMilliseconds(-1));
        }

        /// <inheritdoc />
        public void Stop()
        {
            DisposeTimer();
        }

        /// <summary>
        /// Disposes the timer.
        /// </summary>
        private void DisposeTimer()
        {
            _timer?.Dispose();
            _timer = null;
        }

        /// <summary>
        /// Called when [triggered].
        /// </summary>
        private void OnTriggered()
        {
            DisposeTimer();

            if (Triggered is not null)
            {
                _lastStartDate = DateTime.UtcNow;
                Triggered(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            DisposeTimer();

            _disposed = true;
        }
    }
}
