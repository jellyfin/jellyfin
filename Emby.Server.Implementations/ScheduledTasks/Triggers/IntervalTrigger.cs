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

        /// <summary>
        /// Occurs when [triggered].
        /// </summary>
        public event EventHandler<EventArgs>? Triggered;

        /// <summary>
        /// Gets the options of this task.
        /// </summary>
        public TaskOptions TaskOptions { get; }

        /// <summary>
        /// Stars waiting for the trigger action.
        /// </summary>
        /// <param name="lastResult">The last result.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="taskName">The name of the task.</param>
        /// <param name="isApplicationStartup">if set to <c>true</c> [is application startup].</param>
        public void Start(TaskResult? lastResult, ILogger logger, string taskName, bool isApplicationStartup)
        {
            DisposeTimer();

            DateTime triggerDate;

            if (lastResult == null)
            {
                // Task has never been completed before
                triggerDate = DateTime.UtcNow.AddHours(1);
            }
            else
            {
                triggerDate = new[] { lastResult.EndTimeUtc, _lastStartDate }.Max().Add(_interval);
            }

            if (DateTime.UtcNow > triggerDate)
            {
                triggerDate = DateTime.UtcNow.AddMinutes(1);
            }

            var dueTime = triggerDate - DateTime.UtcNow;
            var maxDueTime = TimeSpan.FromDays(7);

            if (dueTime > maxDueTime)
            {
                dueTime = maxDueTime;
            }

            _timer = new Timer(_ => OnTriggered(), null, dueTime, TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        /// Stops waiting for the trigger action.
        /// </summary>
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

            if (Triggered != null)
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
