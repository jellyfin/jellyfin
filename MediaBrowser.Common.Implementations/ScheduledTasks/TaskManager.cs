using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Implementations.ScheduledTasks
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
        /// Initializes a new instance of the <see cref="TaskManager" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentException">kernel</exception>
        public TaskManager(IApplicationPaths applicationPaths, IJsonSerializer jsonSerializer, ILogger logger)
        {
            ApplicationPaths = applicationPaths;
            JsonSerializer = jsonSerializer;
            Logger = logger;

            ScheduledTasks = new IScheduledTaskWorker[] { };
        }

        /// <summary>
        /// Cancels if running and queue.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void CancelIfRunningAndQueue<T>()
                 where T : IScheduledTask
        {
            var task = ScheduledTasks.First(t => t.ScheduledTask.GetType() == typeof(T));
            ((ScheduledTaskWorker)task).CancelIfRunning();

            QueueScheduledTask<T>();
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
            var type = task.ScheduledTask.GetType();

            lock (_taskQueue)
            {
                // If it's idle just execute immediately
                if (task.State == TaskState.Idle)
                {
                    Execute(task);
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
        /// Adds the tasks.
        /// </summary>
        /// <param name="tasks">The tasks.</param>
        public void AddTasks(IEnumerable<IScheduledTask> tasks)
        {
            var myTasks = ScheduledTasks.ToList();

            var list = tasks.ToList();
            myTasks.AddRange(list.Select(t => new ScheduledTaskWorker(t, ApplicationPaths, this, JsonSerializer, Logger)));

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

        public void Cancel(IScheduledTaskWorker task)
        {
            ((ScheduledTaskWorker)task).Cancel();
        }

        public Task Execute(IScheduledTaskWorker task)
        {
            return ((ScheduledTaskWorker)task).Execute();
        }

        /// <summary>
        /// Called when [task executing].
        /// </summary>
        /// <param name="task">The task.</param>
        internal void OnTaskExecuting(IScheduledTaskWorker task)
        {
            EventHelper.QueueEventIfNotNull(TaskExecuting, this, new GenericEventArgs<IScheduledTaskWorker>
            {
                Argument = task

            }, Logger);
        }

        /// <summary>
        /// Called when [task completed].
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="result">The result.</param>
        internal void OnTaskCompleted(IScheduledTaskWorker task, TaskResult result)
        {
            EventHelper.QueueEventIfNotNull(TaskCompleted, task, new TaskCompletionEventArgs
            {
                Result = result,
                Task = task

            }, Logger);

            ExecuteQueuedTasks();
        }

        /// <summary>
        /// Executes the queued tasks.
        /// </summary>
        private void ExecuteQueuedTasks()
        {
            // Execute queued tasks
            lock (_taskQueue)
            {
                foreach (var type in _taskQueue.ToList())
                {
                    var scheduledTask = ScheduledTasks.First(t => t.ScheduledTask.GetType() == type);

                    if (scheduledTask.State == TaskState.Idle)
                    {
                        Execute(scheduledTask);

                        _taskQueue.Remove(type);
                    }
                }
            }
        }
    }
}
