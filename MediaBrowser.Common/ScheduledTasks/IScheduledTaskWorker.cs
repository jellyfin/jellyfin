using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Common.ScheduledTasks
{
    /// <summary>
    /// Interface IScheduledTaskWorker
    /// </summary>
    public interface IScheduledTaskWorker : IDisposable
    {
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
        /// <exception cref="System.ArgumentNullException">value</exception>
        IEnumerable<ITaskTrigger> Triggers { get; set; }

        /// <summary>
        /// Gets the unique id.
        /// </summary>
        /// <value>The unique id.</value>
        Guid Id { get; }

        /// <summary>
        /// Executes the task
        /// </summary>
        /// <returns>Task.</returns>
        /// <exception cref="System.InvalidOperationException">Cannot execute a Task that is already running</exception>
        Task Execute();

        /// <summary>
        /// Stops the task if it is currently executing
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Cannot cancel a Task unless it is in the Running state.</exception>
        void Cancel();

        /// <summary>
        /// Cancels if running.
        /// </summary>
        void CancelIfRunning();
    }
}