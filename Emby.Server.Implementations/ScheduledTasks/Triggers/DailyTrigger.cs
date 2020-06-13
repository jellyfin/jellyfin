using System;
using System.Threading;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks
{
    /// <summary>
    /// Represents a task trigger that fires everyday.
    /// </summary>
    public class DailyTrigger : ITaskTrigger
    {
        /// <summary>
        /// Get the time of day to trigger the task to run.
        /// </summary>
        /// <value>The time of day.</value>
        public TimeSpan TimeOfDay { get; set; }

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

            var now = DateTime.Now;

            var triggerDate = now.TimeOfDay > TimeOfDay ? now.Date.AddDays(1) : now.Date;
            triggerDate = triggerDate.Add(TimeOfDay);

            var dueTime = triggerDate - now;

            logger.LogInformation("Daily trigger for {Task} set to fire at {TriggerDate:g}, which is {DueTime:g} from now.", taskName, triggerDate, dueTime);

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
            Triggered?.Invoke(this, EventArgs.Empty);
        }
    }
}
