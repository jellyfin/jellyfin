#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Data.Events;

namespace MediaBrowser.Model.Tasks
{
    public interface ITaskManager : IDisposable
    {
        event EventHandler<GenericEventArgs<IScheduledTaskWorker>> TaskExecuting;

        event EventHandler<TaskCompletionEventArgs> TaskCompleted;

        /// <summary>
        /// Gets the list of Scheduled Tasks.
        /// </summary>
        /// <value>The scheduled tasks.</value>
        IScheduledTaskWorker[] ScheduledTasks { get; }

        /// <summary>
        /// Cancels if running and queue.
        /// </summary>
        /// <typeparam name="T">An implementation of <see cref="IScheduledTask" />.</typeparam>
        /// <param name="options">Task options.</param>
        void CancelIfRunningAndQueue<T>(TaskOptions options)
            where T : IScheduledTask;

        /// <summary>
        /// Cancels if running and queue.
        /// </summary>
        /// <typeparam name="T">An implementation of <see cref="IScheduledTask" />.</typeparam>
        void CancelIfRunningAndQueue<T>()
            where T : IScheduledTask;

        /// <summary>
        /// Cancels if running.
        /// </summary>
        /// <typeparam name="T">An implementation of <see cref="IScheduledTask" />.</typeparam>
        void CancelIfRunning<T>()
            where T : IScheduledTask;

        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <typeparam name="T">An implementation of <see cref="IScheduledTask" />.</typeparam>
        /// <param name="options">Task options.</param>
        void QueueScheduledTask<T>(TaskOptions options)
            where T : IScheduledTask;

        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <typeparam name="T">An implementation of <see cref="IScheduledTask" />.</typeparam>
        void QueueScheduledTask<T>()
            where T : IScheduledTask;

        void QueueIfNotRunning<T>()
            where T : IScheduledTask;

        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <param name="task">The <see cref="IScheduledTask" /> to queue.</param>
        /// <param name="options">The <see cref="TaskOptions" /> to use.</param>
        void QueueScheduledTask(IScheduledTask task, TaskOptions options);

        /// <summary>
        /// Adds the tasks.
        /// </summary>
        /// <param name="tasks">The tasks.</param>
        void AddTasks(IEnumerable<IScheduledTask> tasks);

        void Cancel(IScheduledTaskWorker task);

        Task Execute(IScheduledTaskWorker task, TaskOptions options);

        void Execute<T>()
            where T : IScheduledTask;
    }
}
