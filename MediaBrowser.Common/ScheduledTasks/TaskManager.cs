using MediaBrowser.Common.Kernel;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Common.ScheduledTasks
{
    /// <summary>
    /// Class TaskManager
    /// </summary>
    public class TaskManager : BaseManager<IKernel>
    {
        /// <summary>
        /// The _task queue
        /// </summary>
        private readonly List<Type> _taskQueue = new List<Type>();

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskManager" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="logger">The logger.</param>
        public TaskManager(IKernel kernel, ILogger logger)
            : base(kernel)
        {
            _logger = logger;
        }

        /// <summary>
        /// Cancels if running and queue.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void CancelIfRunningAndQueue<T>()
                 where T : IScheduledTask
        {
            Kernel.ScheduledTasks.OfType<T>().First().CancelIfRunning();
            QueueScheduledTask<T>();
        }

        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void QueueScheduledTask<T>()
            where T : IScheduledTask
        {
            var scheduledTask = Kernel.ScheduledTasks.OfType<T>().First();

            QueueScheduledTask(scheduledTask);
        }

        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <param name="task">The task.</param>
        public void QueueScheduledTask(IScheduledTask task)
        {
            var type = task.GetType();

            var scheduledTask = Kernel.ScheduledTasks.First(t => t.GetType() == type);

            lock (_taskQueue)
            {
                // If it's idle just execute immediately
                if (scheduledTask.State == TaskState.Idle)
                {
                    scheduledTask.Execute();
                    return;
                }

                if (!_taskQueue.Contains(type))
                {
                    _logger.Info("Queueing task {0}", type.Name);
                    _taskQueue.Add(type);
                }
                else
                {
                    _logger.Info("Task already queued: {0}", type.Name);
                }
            }
        }

        /// <summary>
        /// Called when [task completed].
        /// </summary>
        /// <param name="task">The task.</param>
        internal void OnTaskCompleted(IScheduledTask task)
        {
            // Execute queued tasks
            lock (_taskQueue)
            {
                var copy = _taskQueue.ToList();

                foreach (var type in copy)
                {
                    var scheduledTask = Kernel.ScheduledTasks.First(t => t.GetType() == type);

                    if (scheduledTask.State == TaskState.Idle)
                    {
                        scheduledTask.Execute();

                        _taskQueue.Remove(type);
                    }
                }
            }
        }
    }
}
