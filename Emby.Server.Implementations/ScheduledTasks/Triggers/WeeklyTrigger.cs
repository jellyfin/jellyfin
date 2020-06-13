using System;
using System.Threading;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks
{
    /// <summary>
    /// Represents a task trigger that fires on a weekly basis
    /// </summary>
    public class WeeklyTrigger : ITaskTrigger
    {
        /// <summary>
        /// Get the time of day to trigger the task to run
        /// </summary>
        /// <value>The time of day.</value>
        public TimeSpan TimeOfDay { get; set; }

        /// <summary>
        /// Gets or sets the day of week.
        /// </summary>
        /// <value>The day of week.</value>
        public DayOfWeek DayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets the options of this task.
        /// </summary>
        public TaskOptions TaskOptions { get; set; }

        /// <summary>
        /// Gets or sets the timer.
        /// </summary>
        /// <value>The timer.</value>
        private Timer Timer { get; set; }

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

            var triggerDate = GetNextTriggerDateTime();

            Timer = new Timer(state => OnTriggered(), null, triggerDate - DateTime.Now, TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        /// Gets the next trigger date time.
        /// </summary>
        /// <returns>DateTime.</returns>
        private DateTime GetNextTriggerDateTime()
        {
            var now = DateTime.Now;

            // If it's on the same day
            if (now.DayOfWeek == DayOfWeek)
            {
                // It's either later today, or a week from now
                return now.TimeOfDay < TimeOfDay ? now.Date.Add(TimeOfDay) : now.Date.AddDays(7).Add(TimeOfDay);
            }

            var triggerDate = now.Date;

            // Walk the date forward until we get to the trigger day
            while (triggerDate.DayOfWeek != DayOfWeek)
            {
                triggerDate = triggerDate.AddDays(1);
            }

            // Return the trigger date plus the time offset
            return triggerDate.Add(TimeOfDay);
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
            if (Triggered != null)
            {
                Triggered(this, EventArgs.Empty);
            }
        }
    }
}
