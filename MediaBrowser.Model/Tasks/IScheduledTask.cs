using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Model.Tasks
{
    /// <summary>
    /// Interface IScheduledTaskWorker.
    /// </summary>
    public interface IScheduledTask
    {
        /// <summary>
        /// Gets the name of the task.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the key of the task.
        /// </summary>
        string Key { get; }

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
        /// Executes the task.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        Task Execute(CancellationToken cancellationToken, IProgress<double> progress);

        /// <summary>
        /// Gets the default triggers that define when the task will run.
        /// </summary>
        /// <returns>The default triggers that define when the task will run.</returns>
        IEnumerable<TaskTriggerInfo> GetDefaultTriggers();
    }
}
