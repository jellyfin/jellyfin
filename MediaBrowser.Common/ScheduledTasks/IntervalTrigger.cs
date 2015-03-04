using System;
using System.Threading;
using MediaBrowser.Model.Events;

namespace MediaBrowser.Common.ScheduledTasks
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
        /// <param name="isApplicationStartup">if set to <c>true</c> [is application startup].</param>
        public void Start(bool isApplicationStartup)
        {
            DisposeTimer();

            Timer = new Timer(state => OnTriggered(), null, Interval, TimeSpan.FromMilliseconds(-1));
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
