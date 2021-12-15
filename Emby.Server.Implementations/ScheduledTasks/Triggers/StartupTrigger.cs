#pragma warning disable CS1591

using System;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Triggers
{
    /// <summary>
    /// Class StartupTaskTrigger.
    /// </summary>
    public sealed class StartupTrigger : ITaskTrigger
    {
        public const int DelayMs = 3000;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupTrigger"/> class.
        /// </summary>
        /// <param name="taskOptions">The options of this task.</param>
        public StartupTrigger(TaskOptions taskOptions)
        {
            TaskOptions = taskOptions;
        }

        /// <summary>
        /// Occurs when [triggered].
        /// </summary>
        public event EventHandler<EventArgs>? Triggered;

        /// <summary>
        /// Gets the options of this task.
        /// </summary>
        public TaskOptions TaskOptions { get; }

        /// <summary>
        /// Stars waiting for the trigger action.
        /// </summary>
        /// <param name="lastResult">The last result.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="taskName">The name of the task.</param>
        /// <param name="isApplicationStartup">if set to <c>true</c> [is application startup].</param>
        public async void Start(TaskResult? lastResult, ILogger logger, string taskName, bool isApplicationStartup)
        {
            if (isApplicationStartup)
            {
                await Task.Delay(DelayMs).ConfigureAwait(false);

                OnTriggered();
            }
        }

        /// <summary>
        /// Stops waiting for the trigger action.
        /// </summary>
        public void Stop()
        {
        }

        /// <summary>
        /// Called when [triggered].
        /// </summary>
        private void OnTriggered()
        {
            Triggered?.Invoke(this, EventArgs.Empty);
        }
    }
}
