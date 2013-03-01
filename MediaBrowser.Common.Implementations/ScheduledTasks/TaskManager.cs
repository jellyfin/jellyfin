using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Common.Implementations.ScheduledTasks
{
    /// <summary>
    /// Class TaskManager
    /// </summary>
    public class TaskManager : ITaskManager
    {
        /// <summary>
        /// Gets the list of Scheduled Tasks
        /// </summary>
        /// <value>The scheduled tasks.</value>
        public IScheduledTaskWorker[] ScheduledTasks { get; private set; }

        /// <summary>
        /// The _task queue
        /// </summary>
        private readonly List<Type> _taskQueue = new List<Type>();

        /// <summary>
        /// Gets or sets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        private IJsonSerializer JsonSerializer { get; set; }

        /// <summary>
        /// Gets or sets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        private IApplicationPaths ApplicationPaths { get; set; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the server manager.
        /// </summary>
        /// <value>The server manager.</value>
        private IServerManager ServerManager { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskManager" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="serverManager">The server manager.</param>
        /// <exception cref="System.ArgumentException">kernel</exception>
        public TaskManager(IApplicationPaths applicationPaths, IJsonSerializer jsonSerializer, ILogger logger, IServerManager serverManager)
        {
            ApplicationPaths = applicationPaths;
            JsonSerializer = jsonSerializer;
            Logger = logger;
            ServerManager = serverManager;

            ScheduledTasks = new IScheduledTaskWorker[] { };
        }

        /// <summary>
        /// Cancels if running and queue.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void CancelIfRunningAndQueue<T>()
                 where T : IScheduledTask
        {
            ScheduledTasks.First(t => t.ScheduledTask.GetType() == typeof(T)).CancelIfRunning();
            QueueScheduledTask<T>();
        }

        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void QueueScheduledTask<T>()
            where T : IScheduledTask
        {
            var scheduledTask = ScheduledTasks.First(t => t.ScheduledTask.GetType() == typeof(T));

            QueueScheduledTask(scheduledTask);
        }

        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <param name="task">The task.</param>
        public void QueueScheduledTask(IScheduledTask task)
        {
            var scheduledTask = ScheduledTasks.First(t => t.ScheduledTask.GetType() == task.GetType());

            QueueScheduledTask(scheduledTask);
        }
        
        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <param name="task">The task.</param>
        private void QueueScheduledTask(IScheduledTaskWorker task)
        {
            var type = task.GetType();

            lock (_taskQueue)
            {
                // If it's idle just execute immediately
                if (task.State == TaskState.Idle)
                {
                    task.Execute();
                    return;
                }

                if (!_taskQueue.Contains(type))
                {
                    Logger.Info("Queueing task {0}", type.Name);
                    _taskQueue.Add(type);
                }
                else
                {
                    Logger.Info("Task already queued: {0}", type.Name);
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
        /// Adds the tasks.
        /// </summary>
        /// <param name="tasks">The tasks.</param>
        public void AddTasks(IEnumerable<IScheduledTask> tasks)
        {
            var myTasks = ScheduledTasks.ToList();

            myTasks.AddRange(tasks.Select(t => new ScheduledTaskWorker(t, ApplicationPaths, this, JsonSerializer, Logger, ServerManager)));

            ScheduledTasks = myTasks.ToArray();
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
    }
}
