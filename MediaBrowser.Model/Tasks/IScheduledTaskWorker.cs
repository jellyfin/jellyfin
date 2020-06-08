#nullable disable
using System;
using MediaBrowser.Model.Events;

namespace MediaBrowser.Model.Tasks
{
    /// <summary>
    /// Interface IScheduledTaskWorker.
    /// </summary>
    public interface IScheduledTaskWorker : IDisposable
    {
        /// <summary>
        /// Occurs when [task progress].
        /// </summary>
        event EventHandler<GenericEventArgs<double>> TaskProgress;

        /// <summary>
        /// Gets or sets the scheduled task.
        /// </summary>
        /// <value>The scheduled task.</value>
        IScheduledTask ScheduledTask { get; }

        /// <summary>
        /// Gets the last execution result.
        /// </summary>
        /// <value>The last execution result.</value>
        TaskResult LastExecutionResult { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        string Description { get; }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        string Category { get; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>The state.</value>
        TaskState State { get; }

        /// <summary>
        /// Gets the current progress.
        /// </summary>
        /// <value>The current progress.</value>
        double? CurrentProgress { get; }

        /// <summary>
        /// Gets the triggers that define when the task will run
        /// </summary>
        /// <value>The triggers.</value>
        /// <exception cref="ArgumentNullException">value</exception>
        TaskTriggerInfo[] Triggers { get; set; }

        /// <summary>
        /// Gets the unique id.
        /// </summary>
        /// <value>The unique id.</value>
        string Id { get; }

        /// <summary>
        /// Reloads the trigger events.
        /// </summary>
        void ReloadTriggerEvents();
    }
}
