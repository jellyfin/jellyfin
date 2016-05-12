using MediaBrowser.Model.Events;
using MediaBrowser.Model.Tasks;
using System;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Common.ScheduledTasks
{
    /// <summary>
    /// Interface ITaskTrigger
    /// </summary>
    public interface ITaskTrigger
    {
        /// <summary>
        /// Fires when the trigger condition is satisfied and the task should run
        /// </summary>
        event EventHandler<GenericEventArgs<TaskExecutionOptions>> Triggered;

        /// <summary>
        /// Stars waiting for the trigger action
        /// </summary>
        /// <param name="lastResult">The last result.</param>
        /// <param name="isApplicationStartup">if set to <c>true</c> [is application startup].</param>
        void Start(TaskResult lastResult, ILogger logger, string taskName, bool isApplicationStartup);

        /// <summary>
        /// Stops waiting for the trigger action
        /// </summary>
        void Stop();

        /// <summary>
        /// Gets or sets the execution properties of this task.
        /// </summary>
        /// <value>
        /// The execution properties of this task.
        /// </value>
        TaskExecutionOptions TaskOptions { get; set; }
    }
}