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
        /// <param name="options">Task options.</param>
        void CancelIfRunningAndQueue<T>(TaskExecutionOptions options)
            where T : IScheduledTask;

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
        /// <param name="options">Task options.</param>
        void QueueScheduledTask<T>(TaskExecutionOptions options)
            where T : IScheduledTask;

        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        void QueueScheduledTask<T>()
            where T : IScheduledTask;

        void QueueIfNotRunning<T>()
            where T : IScheduledTask;
        
        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="options">The task run options.</param>
        void QueueScheduledTask(IScheduledTask task, TaskExecutionOptions options = null);

        /// <summary>
        /// Adds the tasks.
        /// </summary>
        /// <param name="tasks">The tasks.</param>
        void AddTasks(IEnumerable<IScheduledTask> tasks);

        void Cancel(IScheduledTaskWorker task);
        Task Execute(IScheduledTaskWorker task, TaskExecutionOptions options = null);

        void Execute<T>()
            where T : IScheduledTask;
        
        event EventHandler<GenericEventArgs<IScheduledTaskWorker>> TaskExecuting;
        event EventHandler<TaskCompletionEventArgs> TaskCompleted;

        bool SuspendTriggers { get; set; }
    }
}