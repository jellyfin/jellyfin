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
        private const int DelayMs = 3000;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupTrigger"/> class.
        /// </summary>
        /// <param name="taskOptions">The options of this task.</param>
        public StartupTrigger(TaskOptions taskOptions)
        {
            TaskOptions = taskOptions;
        }

        /// <inheritdoc />
        public event EventHandler<EventArgs>? Triggered;

        /// <inheritdoc />
        public TaskOptions TaskOptions { get; }

        /// <inheritdoc />
        public async void Start(TaskResult? lastResult, ILogger logger, string taskName, bool isApplicationStartup)
        {
            if (isApplicationStartup)
            {
                await Task.Delay(DelayMs).ConfigureAwait(false);

                OnTriggered();
            }
        }

        /// <inheritdoc />
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
