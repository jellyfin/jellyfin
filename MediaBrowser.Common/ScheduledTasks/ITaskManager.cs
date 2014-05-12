using MediaBrowser.Model.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Common.ScheduledTasks
{
    public interface ITaskManager : IDisposable
    {
        /// <summary>
        /// Gets the list of Scheduled Tasks
        /// </summary>
        /// <value>The scheduled tasks.</value>
        IScheduledTaskWorker[] ScheduledTasks { get; }

        /// <summary>
        /// Cancels if running and queue.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        void CancelIfRunningAndQueue<T>()
            where T : IScheduledTask;

        /// <summary>
        /// Cancels if running.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        void CancelIfRunning<T>()
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

        void Cancel(IScheduledTaskWorker task);
        Task Execute(IScheduledTaskWorker task);

        event EventHandler<GenericEventArgs<IScheduledTaskWorker>> TaskExecuting;
        event EventHandler<TaskCompletionEventArgs> TaskCompleted;
    }
}