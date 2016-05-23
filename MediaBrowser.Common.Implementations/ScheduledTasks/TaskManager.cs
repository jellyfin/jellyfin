using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonIO;
using Microsoft.Win32;

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
        private readonly ConcurrentQueue<Tuple<Type, TaskExecutionOptions>> _taskQueue =
            new ConcurrentQueue<Tuple<Type, TaskExecutionOptions>>();

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
        private readonly IFileSystem _fileSystem;

        private bool _suspendTriggers;

        public bool SuspendTriggers
        {
            get { return _suspendTriggers; }
            set
            {
                Logger.Info("Setting SuspendTriggers to {0}", value);
                var executeQueued = _suspendTriggers && !value;

                _suspendTriggers = value;

                if (executeQueued)
                {
                    ExecuteQueuedTasks();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskManager" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentException">kernel</exception>
        public TaskManager(IApplicationPaths applicationPaths, IJsonSerializer jsonSerializer, ILogger logger, IFileSystem fileSystem)
        {
            ApplicationPaths = applicationPaths;
            JsonSerializer = jsonSerializer;
            Logger = logger;
            _fileSystem = fileSystem;

            ScheduledTasks = new IScheduledTaskWorker[] { };
        }

        private void BindToSystemEvent()
        {
            try
            {
                SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            }
            catch
            {

            }
        }

        void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            foreach (var task in ScheduledTasks)
            {
                task.ReloadTriggerEvents();
            }
        }

        /// <summary>
        /// Cancels if running and queue.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options">Task options.</param>
        public void CancelIfRunningAndQueue<T>(TaskExecutionOptions options)
                 where T : IScheduledTask
        {
            var task = ScheduledTasks.First(t => t.ScheduledTask.GetType() == typeof(T));
            ((ScheduledTaskWorker)task).CancelIfRunning();

            QueueScheduledTask<T>(options);
        }

        public void CancelIfRunningAndQueue<T>()
               where T : IScheduledTask
        {
            CancelIfRunningAndQueue<T>(new TaskExecutionOptions());
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
        public void QueueScheduledTask<T>(TaskExecutionOptions options)
            where T : IScheduledTask
        {
            var scheduledTask = ScheduledTasks.FirstOrDefault(t => t.ScheduledTask.GetType() == typeof(T));

            if (scheduledTask == null)
            {
                Logger.Error("Unable to find scheduled task of type {0} in QueueScheduledTask.", typeof(T).Name);
            }
            else
            {
                QueueScheduledTask(scheduledTask, options);
            }
        }

        public void QueueScheduledTask<T>()
            where T : IScheduledTask
        {
            QueueScheduledTask<T>(new TaskExecutionOptions());
        }

        public void QueueIfNotRunning<T>()
            where T : IScheduledTask
        {
            var task = ScheduledTasks.First(t => t.ScheduledTask.GetType() == typeof(T));

            if (task.State != TaskState.Running)
            {
                QueueScheduledTask<T>(new TaskExecutionOptions());
            }
        }

        public void Execute<T>()
            where T : IScheduledTask
        {
            var scheduledTask = ScheduledTasks.FirstOrDefault(t => t.ScheduledTask.GetType() == typeof(T));

            if (scheduledTask == null)
            {
                Logger.Error("Unable to find scheduled task of type {0} in Execute.", typeof(T).Name);
            }
            else
            {
                var type = scheduledTask.ScheduledTask.GetType();

                Logger.Info("Queueing task {0}", type.Name);

                lock (_taskQueue)
                {
                    if (scheduledTask.State == TaskState.Idle)
                    {
                        Execute(scheduledTask, new TaskExecutionOptions());
                    }
                }
            }
        }

        /// <summary>
        /// Queues the scheduled task.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="options">The task options.</param>
        public void QueueScheduledTask(IScheduledTask task, TaskExecutionOptions options)
        {
            var scheduledTask = ScheduledTasks.FirstOrDefault(t => t.ScheduledTask.GetType() == task.GetType());

            if (scheduledTask == null)
            {
                Logger.Error("Unable to find scheduled task of type {0} in QueueScheduledTask.", task.GetType().Name);
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
        private void QueueScheduledTask(IScheduledTaskWorker task, TaskExecutionOptions options)
        {
            var type = task.ScheduledTask.GetType();

            Logger.Info("Queueing task {0}", type.Name);

            lock (_taskQueue)
            {
                if (task.State == TaskState.Idle && !SuspendTriggers)
                {
                    Execute(task, options);
                    return;
                }

                _taskQueue.Enqueue(new Tuple<Type, TaskExecutionOptions>(type, options));
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
            myTasks.AddRange(list.Select(t => new ScheduledTaskWorker(t, ApplicationPaths, this, JsonSerializer, Logger, _fileSystem)));

            ScheduledTasks = myTasks.ToArray();

            BindToSystemEvent();
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

        public Task Execute(IScheduledTaskWorker task, TaskExecutionOptions options)
        {
            return ((ScheduledTaskWorker)task).Execute(options);
        }

        /// <summary>
        /// Called when [task executing].
        /// </summary>
        /// <param name="task">The task.</param>
        internal void OnTaskExecuting(IScheduledTaskWorker task)
        {
            EventHelper.FireEventIfNotNull(TaskExecuting, this, new GenericEventArgs<IScheduledTaskWorker>
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
            EventHelper.FireEventIfNotNull(TaskCompleted, task, new TaskCompletionEventArgs
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
            if (SuspendTriggers)
            {
                return;
            }

            Logger.Info("ExecuteQueuedTasks");

            // Execute queued tasks
            lock (_taskQueue)
            {
                var list = new List<Tuple<Type, TaskExecutionOptions>>();

                Tuple<Type, TaskExecutionOptions> item;
                while (_taskQueue.TryDequeue(out item))
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
