using System;

namespace MediaBrowser.Model.Tasks
{
    /// <summary>
    /// Class containing event arguments for task completion.
    /// </summary>
    public class TaskCompletionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCompletionEventArgs"/> class.
        /// </summary>
        /// <param name="task">Instance of the <see cref="IScheduledTaskWorker"/> interface.</param>
        /// <param name="result">The task result.</param>
        public TaskCompletionEventArgs(IScheduledTaskWorker task, TaskResult result)
        {
            Task = task;
            Result = result;
        }

        /// <summary>
        /// Gets the task.
        /// </summary>
        /// <value>The task.</value>
        public IScheduledTaskWorker Task { get; }

        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <value>The result.</value>
        public TaskResult Result { get; }
    }
}
