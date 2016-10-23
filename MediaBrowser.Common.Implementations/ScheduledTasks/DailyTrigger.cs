using MediaBrowser.Model.Events;
using MediaBrowser.Model.Tasks;
using System;
using System.Globalization;
using System.Threading;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Common.ScheduledTasks
{
    /// <summary>
    /// Represents a task trigger that fires everyday
    /// </summary>
    public class DailyTrigger : ITaskTrigger
    {
        /// <summary>
        /// Get the time of day to trigger the task to run
        /// </summary>
        /// <value>The time of day.</value>
        public TimeSpan TimeOfDay { get; set; }

        /// <summary>
        /// Gets or sets the timer.
        /// </summary>
        /// <value>The timer.</value>
        private Timer Timer { get; set; }

        /// <summary>
        /// Gets the execution properties of this task.
        /// </summary>
        /// <value>
        /// The execution properties of this task.
        /// </value>
        public TaskExecutionOptions TaskOptions { get; set; }

        /// <summary>
        /// Stars waiting for the trigger action
        /// </summary>
        /// <param name="lastResult">The last result.</param>
        /// <param name="isApplicationStartup">if set to <c>true</c> [is application startup].</param>
        public void Start(TaskResult lastResult, ILogger logger, string taskName, bool isApplicationStartup)
        {
            DisposeTimer();

            var now = DateTime.Now;

            var triggerDate = now.TimeOfDay > TimeOfDay ? now.Date.AddDays(1) : now.Date;
            triggerDate = triggerDate.Add(TimeOfDay);

            var dueTime = triggerDate - now;

            logger.Info("Daily trigger for {0} set to fire at {1}, which is {2} minutes from now.", taskName, triggerDate.ToString(), dueTime.TotalMinutes.ToString(CultureInfo.InvariantCulture));

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
        public event EventHandler<GenericEventArgs<TaskExecutionOptions>> Triggered;

        /// <summary>
        /// Called when [triggered].
        /// </summary>
        private void OnTriggered()
        {
            if (Triggered != null)
            {
                Triggered(this, new GenericEventArgs<TaskExecutionOptions>(TaskOptions));
            }
        }
    }
}
