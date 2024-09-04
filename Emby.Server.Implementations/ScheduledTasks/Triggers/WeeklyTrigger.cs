using System;
using System.Threading;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Triggers
{
    /// <summary>
    /// Represents a task trigger that fires on a weekly basis.
    /// </summary>
    public sealed class WeeklyTrigger : ITaskTrigger, IDisposable
    {
        private readonly TimeSpan _timeOfDay;
        private readonly DayOfWeek _dayOfWeek;
        private Timer? _timer;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeeklyTrigger"/> class.
        /// </summary>
        /// <param name="timeofDay">The time of day to trigger the task to run.</param>
        /// <param name="dayOfWeek">The day of week.</param>
        /// <param name="taskOptions">The options of this task.</param>
        public WeeklyTrigger(TimeSpan timeofDay, DayOfWeek dayOfWeek, TaskOptions taskOptions)
        {
            _timeOfDay = timeofDay;
            _dayOfWeek = dayOfWeek;
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

            var triggerDate = GetNextTriggerDateTime();

            _timer = new Timer(_ => OnTriggered(), null, triggerDate - DateTime.Now, TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        /// Gets the next trigger date time.
        /// </summary>
        /// <returns>DateTime.</returns>
        private DateTime GetNextTriggerDateTime()
        {
            var now = DateTime.Now;

            // If it's on the same day
            if (now.DayOfWeek == _dayOfWeek)
            {
                // It's either later today, or a week from now
                return now.TimeOfDay < _timeOfDay ? now.Date.Add(_timeOfDay) : now.Date.AddDays(7).Add(_timeOfDay);
            }

            var triggerDate = now.Date;

            // Walk the date forward until we get to the trigger day
            while (triggerDate.DayOfWeek != _dayOfWeek)
            {
                triggerDate = triggerDate.AddDays(1);
            }

            // Return the trigger date plus the time offset
            return triggerDate.Add(_timeOfDay);
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
            Triggered?.Invoke(this, EventArgs.Empty);
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
