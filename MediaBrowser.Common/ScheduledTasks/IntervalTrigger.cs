using System;
using System.Threading;

namespace MediaBrowser.Common.ScheduledTasks
{
    /// <summary>
    /// Represents a task trigger that runs repeatedly on an interval
    /// </summary>
    public class IntervalTrigger : BaseTaskTrigger
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
        /// Stars waiting for the trigger action
        /// </summary>
        protected internal override void Start()
        {
            DisposeTimer();

            Timer = new Timer(state => OnTriggered(), null, Interval, TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        /// Stops waiting for the trigger action
        /// </summary>
        protected internal override void Stop()
        {
            DisposeTimer();
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool dispose)
        {
            if (dispose)
            {
                DisposeTimer();
            }

            base.Dispose(dispose);
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
    }
}
