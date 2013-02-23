using System;
using System.Threading;

namespace MediaBrowser.Common.ScheduledTasks
{
    /// <summary>
    /// Represents a task trigger that fires everyday
    /// </summary>
    public class DailyTrigger : BaseTaskTrigger
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
        /// Stars waiting for the trigger action
        /// </summary>
        /// <param name="isApplicationStartup">if set to <c>true</c> [is application startup].</param>
        protected internal override void Start(bool isApplicationStartup)
        {
            DisposeTimer();

            var now = DateTime.Now;

            var triggerDate = now.TimeOfDay > TimeOfDay ? now.Date.AddDays(1) : now.Date;
            triggerDate = triggerDate.Add(TimeOfDay);

            Timer = new Timer(state => OnTriggered(), null, triggerDate - now, TimeSpan.FromMilliseconds(-1));
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
