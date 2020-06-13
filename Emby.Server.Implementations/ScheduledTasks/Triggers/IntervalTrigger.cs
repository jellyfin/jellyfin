using System;
using System.Linq;
using System.Threading;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks
{
    /// <summary>
    /// Represents a task trigger that runs repeatedly on an interval
    /// </summary>
    public class IntervalTrigger : ITaskTrigger
    {
        /// <summary>
        /// Gets or sets the interval.
        /// </summary>
        /// <value>The interval.</value>
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// Gets or sets the options of this task.
        /// </summary>
        public TaskOptions TaskOptions { get; set; }

        /// <summary>
        /// Gets or sets the timer.
        /// </summary>
        /// <value>The timer.</value>
        private Timer Timer { get; set; }

        private DateTime _lastStartDate;

        /// <summary>
        /// Stars waiting for the trigger action
        /// </summary>
        /// <param name="lastResult">The last result.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="taskName">The name of the task.</param>
        /// <param name="isApplicationStartup">if set to <c>true</c> [is application startup].</param>
        public void Start(TaskResult lastResult, ILogger logger, string taskName, bool isApplicationStartup)
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
                triggerDate = new[] { lastResult.EndTimeUtc, _lastStartDate }.Max().Add(Interval);
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

            Timer = new Timer(state => OnTriggered(), null, dueTime, TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        /// Stops waiting for the trigger action
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
            if (Timer != null)
            {
                Timer.Dispose();
            }
        }

        /// <summary>
        /// Occurs when [triggered].
        /// </summary>
        public event EventHandler<EventArgs> Triggered;

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
    }
}
