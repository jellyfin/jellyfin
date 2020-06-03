#pragma warning disable CS1591

using System;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks
{
    /// <summary>
    /// Class StartupTaskTrigger.
    /// </summary>
    public class StartupTrigger : ITaskTrigger
    {
        public int DelayMs { get; set; }

        /// <summary>
        /// Gets or sets the options of this task.
        /// </summary>
        public TaskOptions TaskOptions { get; set; }

        public StartupTrigger()
        {
            DelayMs = 3000;
        }

        /// <summary>
        /// Stars waiting for the trigger action
        /// </summary>
        /// <param name="lastResult">The last result.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="taskName">The name of the task.</param>
        /// <param name="isApplicationStartup">if set to <c>true</c> [is application startup].</param>
        public async void Start(TaskResult lastResult, ILogger logger, string taskName, bool isApplicationStartup)
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
