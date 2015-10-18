using MediaBrowser.Model.Events;
using MediaBrowser.Model.Tasks;
using System;
using System.Threading;

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
        /// Gets or sets the first run delay.
        /// </summary>
        /// <value>The first run delay.</value>
        public TimeSpan FirstRunDelay { get; set; }

        public IntervalTrigger()
        {
            FirstRunDelay = TimeSpan.FromHours(1);
        }

        /// <summary>
        /// Stars waiting for the trigger action
        /// </summary>
        /// <param name="lastResult">The last result.</param>
        /// <param name="isApplicationStartup">if set to <c>true</c> [is application startup].</param>
        public void Start(TaskResult lastResult, bool isApplicationStartup)
        {
            DisposeTimer();

            var triggerDate = lastResult != null ?
                            lastResult.EndTimeUtc.Add(Interval) :
                            DateTime.UtcNow.Add(FirstRunDelay);

            if (DateTime.UtcNow > triggerDate)
            {
                if (isApplicationStartup)
                {
                    triggerDate = DateTime.UtcNow.AddMinutes(1);
                }
                else
                {
                    triggerDate = DateTime.UtcNow.AddMinutes(1);
                }
            }

            Timer = new Timer(state => OnTriggered(), null, triggerDate - DateTime.UtcNow, TimeSpan.FromMilliseconds(-1));
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
