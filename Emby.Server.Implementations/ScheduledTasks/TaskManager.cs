#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks
{
    /// <summary>
    /// Class TaskManager
    /// </summary>
    public class TaskManager : ITaskManager
    {
        public event EventHandler<GenericEventArgs<IScheduledTaskWorker>> TaskExecuting;
        public event EventHandler<TaskCompletionEventArgs> TaskCompleted;

        /// <summary>
        /// Gets the list of Scheduled Tasks
        /// </summary>
        /// <value>The scheduled tasks.</value>
        public IScheduledTaskWorker[] ScheduledTasks { get; private set; }

        /// <summary>
        /// The _task queue
        /// </summary>
        private readonly ConcurrentQueue<Tuple<Type, TaskOptions>> _taskQueue =
            new ConcurrentQueue<Tuple<Type, TaskOptions>>();

        private readonly IJsonSerializer _jsonSerializer;
        private readonly IApplicationPaths _applicationPaths;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskManager" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The filesystem manager.</param>
        public TaskManager(
            IApplicationPaths applicationPaths,
            IJsonSerializer jsonSerializer,
            ILogger<TaskManager> logger,
            IFileSystem fileSystem)
        {
            _applicationPaths = applicationPaths;
            _jsonSerializer = jsonSerializer;
            _logger = logger;
            _fileSystem = fileSystem;

            ScheduledTasks = Array.Empty<IScheduledTaskWorker>();
        }

        /// <summary>
        /// Cancels if running and queue.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options">Task options.</param>
        public void CancelIfRunningAndQueue<T>(TaskOptions options)
            where T : IScheduledTask
        {
            var task = ScheduledTasks.First(t => t.ScheduledTask.GetType() == typeof(T));
            ((ScheduledTaskWorker)task).CancelIfRunning();

            QueueScheduledTask<T>(options);
        }

        public void CancelIfRunningAndQueue<T>()
               where T : IScheduledTask
        {
            CancelIfRunningAndQueue<T>(new TaskOptions());
        }

        /// <summary>
        /// Cancels if running
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void CancelIfRunning<T>()
                 where T : IScheduledTask
        {
            var task = ScheduledTasks.First(t => t.ScheduledTask.GetType() == typeof(T));
            ((ScheduledTaskWorker)task).CancelIfRunning();
        }

        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options">Task options</param>
        public void QueueScheduledTask<T>(TaskOptions options)
            where T : IScheduledTask
        {
            var scheduledTask = ScheduledTasks.FirstOrDefault(t => t.ScheduledTask.GetType() == typeof(T));

            if (scheduledTask == null)
            {
                _logger.LogError("Unable to find scheduled task of type {0} in QueueScheduledTask.", typeof(T).Name);
            }
            else
            {
                QueueScheduledTask(scheduledTask, options);
            }
        }

        public void QueueScheduledTask<T>()
            where T : IScheduledTask
        {
            QueueScheduledTask<T>(new TaskOptions());
        }

        public void QueueIfNotRunning<T>()
            where T : IScheduledTask
        {
            var task = ScheduledTasks.First(t => t.ScheduledTask.GetType() == typeof(T));

            if (task.State != TaskState.Running)
            {
                QueueScheduledTask<T>(new TaskOptions());
            }
        }

        public void Execute<T>()
            where T : IScheduledTask
        {
            var scheduledTask = ScheduledTasks.FirstOrDefault(t => t.ScheduledTask.GetType() == typeof(T));

            if (scheduledTask == null)
            {
                _logger.LogError("Unable to find scheduled task of type {0} in Execute.", typeof(T).Name);
            }
            else
            {
                var type = scheduledTask.ScheduledTask.GetType();

                _logger.LogInformation("Queueing task {0}", type.Name);

                lock (_taskQueue)
                {
                    if (scheduledTask.State == TaskState.Idle)
                    {
                        Execute(scheduledTask, new TaskOptions());
                    }
                }
            }
        }

        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="options">The task options.</param>
        public void QueueScheduledTask(IScheduledTask task, TaskOptions options)
        {
            var scheduledTask = ScheduledTasks.FirstOrDefault(t => t.ScheduledTask.GetType() == task.GetType());

            if (scheduledTask == null)
            {
                _logger.LogError("Unable to find scheduled task of type {0} in QueueScheduledTask.", task.GetType().Name);
            }
            else
            {
                QueueScheduledTask(scheduledTask, options);
            }
        }

        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="options">The task options.</param>
        private void QueueScheduledTask(IScheduledTaskWorker task, TaskOptions options)
        {
            var type = task.ScheduledTask.GetType();

            _logger.LogInformation("Queueing task {0}", type.Name);

            lock (_taskQueue)
            {
                if (task.State == TaskState.Idle)
                {
                    Execute(task, options);
                    return;
                }

                _taskQueue.Enqueue(new Tuple<Type, TaskOptions>(type, options));
            }
        }

        /// <summary>
        /// Adds the tasks.
        /// </summary>
        /// <param name="tasks">The tasks.</param>
        public void AddTasks(IEnumerable<IScheduledTask> tasks)
        {
            var list = tasks.Select(t => new ScheduledTaskWorker(t, _applicationPaths, this, _jsonSerializer, _logger));

            ScheduledTasks = ScheduledTasks.Concat(list).ToArray();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
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

        public void Cancel(IScheduledTaskWorker task)
        {
            ((ScheduledTaskWorker)task).Cancel();
        }

        public Task Execute(IScheduledTaskWorker task, TaskOptions options)
        {
            return ((ScheduledTaskWorker)task).Execute(options);
        }

        /// <summary>
        /// Called when [task executing].
        /// </summary>
        /// <param name="task">The task.</param>
        internal void OnTaskExecuting(IScheduledTaskWorker task)
        {
            TaskExecuting?.Invoke(this, new GenericEventArgs<IScheduledTaskWorker>(task));
        }

        /// <summary>
        /// Called when [task completed].
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="result">The result.</param>
        internal void OnTaskCompleted(IScheduledTaskWorker task, TaskResult result)
        {
            TaskCompleted?.Invoke(task, new TaskCompletionEventArgs(task, result));

            ExecuteQueuedTasks();
        }

        /// <summary>
        /// Executes the queued tasks.
        /// </summary>
        private void ExecuteQueuedTasks()
        {
            _logger.LogInformation("ExecuteQueuedTasks");

            // Execute queued tasks
            lock (_taskQueue)
            {
                var list = new List<Tuple<Type, TaskOptions>>();

                while (_taskQueue.TryDequeue(out var item))
                {
                    if (list.All(i => i.Item1 != item.Item1))
                    {
                        list.Add(item);
                    }
                }

                foreach (var enqueuedType in list)
                {
                    var scheduledTask = ScheduledTasks.First(t => t.ScheduledTask.GetType() == enqueuedType.Item1);

                    if (scheduledTask.State == TaskState.Idle)
                    {
                        Execute(scheduledTask, enqueuedType.Item2);
                    }
                }
            }
        }
    }
}
