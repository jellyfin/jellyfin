using System;
using System.Threading.Tasks;

namespace MediaBrowser.Common.ScheduledTasks
{
    /// <summary>
    /// Class StartupTaskTrigger
    /// </summary>
    public class StartupTrigger : ITaskTrigger
    {
        public int DelayMs { get; set; }

        public StartupTrigger()
        {
            DelayMs = 3000;
        }

        /// <summary>
        /// Stars waiting for the trigger action
        /// </summary>
        /// <param name="isApplicationStartup">if set to <c>true</c> [is application startup].</param>
        public async void Start(bool isApplicationStartup)
        {
            if (isApplicationStartup)
            {
                await Task.Delay(DelayMs).ConfigureAwait(false);

                OnTriggered();
            }
        }

        /// <summary>
        /// Stops waiting for the trigger action
        /// </summary>
        public void Stop()
        {
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
