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
    internal class TaskManager : ITaskManager
    {
        /// <summary>
        /// Gets the list of Scheduled Tasks
        /// </summary>
        /// <value>The scheduled tasks.</value>
        public IScheduledTask[] ScheduledTasks { get; private set; }

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
        /// <param name="logger">The logger.</param>
        public TaskManager(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentException("logger");
            }

            _logger = logger;

            ScheduledTasks = new IScheduledTask[] {};
        }

        /// <summary>
        /// Cancels if running and queue.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void CancelIfRunningAndQueue<T>()
                 where T : IScheduledTask
        {
            ScheduledTasks.OfType<T>().First().CancelIfRunning();
            QueueScheduledTask<T>();
        }

        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void QueueScheduledTask<T>()
            where T : IScheduledTask
        {
            var scheduledTask = ScheduledTasks.OfType<T>().First();

            QueueScheduledTask(scheduledTask);
        }

        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <param name="task">The task.</param>
        public void QueueScheduledTask(IScheduledTask task)
        {
            var type = task.GetType();

            var scheduledTask = ScheduledTasks.First(t => t.GetType() == type);

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
        public void OnTaskCompleted(IScheduledTask task)
        {
            // Execute queued tasks
            lock (_taskQueue)
            {
                var copy = _taskQueue.ToList();

                foreach (var type in copy)
                {
                    var scheduledTask = ScheduledTasks.First(t => t.GetType() == type);

                    if (scheduledTask.State == TaskState.Idle)
                    {
                        scheduledTask.Execute();

                        _taskQueue.Remove(type);
                    }
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            foreach (var task in ScheduledTasks)
            {
                task.Dispose();
            }
        }

        /// <summary>
        /// Adds the tasks.
        /// </summary>
        /// <param name="tasks">The tasks.</param>
        public void AddTasks(IEnumerable<IScheduledTask> tasks)
        {
            var myTasks = ScheduledTasks.ToList();

            myTasks.AddRange(tasks);

            ScheduledTasks = myTasks.ToArray();
        }
    }
}
