using System;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Model.Tasks
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