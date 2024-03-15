using System;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Model.Tasks
{
    /// <summary>
    /// Interface ITaskTrigger.
    /// </summary>
    public interface ITaskTrigger
    {
        /// <summary>
        /// Fires when the trigger condition is satisfied and the task should run.
        /// </summary>
        event EventHandler<EventArgs>? Triggered;

        /// <summary>
        /// Gets the options of this task.
        /// </summary>
        TaskOptions TaskOptions { get; }

        /// <summary>
        /// Stars waiting for the trigger action.
        /// </summary>
        /// <param name="lastResult">Result of the last run triggered task.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="taskName">The name of the task.</param>
        /// <param name="isApplicationStartup">Whether or not this is fired during startup.</param>
        void Start(TaskResult? lastResult, ILogger logger, string taskName, bool isApplicationStartup);

        /// <summary>
        /// Stops waiting for the trigger action.
        /// </summary>
        void Stop();
    }
}
