using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Common.ScheduledTasks
{
    public interface ITaskManager : IDisposable
    {
        /// <summary>
        /// Gets the list of Scheduled Tasks
        /// </summary>
        /// <value>The scheduled tasks.</value>
        IScheduledTask[] ScheduledTasks { get; }

        /// <summary>
        /// Cancels if running and queue.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        void CancelIfRunningAndQueue<T>()
            where T : IScheduledTask;

        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        void QueueScheduledTask<T>()
            where T : IScheduledTask;

        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <param name="task">The task.</param>
        void QueueScheduledTask(IScheduledTask task);

        /// <summary>
        /// Adds the tasks.
        /// </summary>
        /// <param name="tasks">The tasks.</param>
        void AddTasks(IEnumerable<IScheduledTask> tasks);

        /// <summary>
        /// Called when [task completed].
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        /// <param name="status">The status.</param>
        void OnTaskCompleted(IScheduledTask task, DateTime startTime, DateTime endTime, TaskCompletionStatus status);

        /// <summary>
        /// Gets the last execution result.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns>TaskResult.</returns>
        TaskResult GetLastExecutionResult(IScheduledTask task);

        /// <summary>
        /// Loads the triggers.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        IEnumerable<ITaskTrigger> LoadTriggers(IScheduledTask task);

        /// <summary>
        /// Saves the triggers.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="triggers">The triggers.</param>
        void SaveTriggers(IScheduledTask task, IEnumerable<ITaskTrigger> triggers);
    }
}