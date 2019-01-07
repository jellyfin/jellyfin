using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Tasks;

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

        private readonly ISystemEvents _systemEvents;

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private ILogger Logger { get; set; }
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskManager" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentException">kernel</exception>
        public TaskManager(IApplicationPaths applicationPaths, IJsonSerializer jsonSerializer, ILogger logger, IFileSystem fileSystem, ISystemEvents systemEvents)
        {
            ApplicationPaths = applicationPaths;
            JsonSerializer = jsonSerializer;
            Logger = logger;
            _fileSystem = fileSystem;
            _systemEvents = systemEvents;

            ScheduledTasks = new IScheduledTaskWorker[] { };
        }

        private void BindToSystemEvent()
        {
            _systemEvents.Resume += _systemEvents_Resume;
        }

        private void _systemEvents_Resume(object sender, EventArgs e)
        {
            foreach (var task in ScheduledTasks)
            {
                task.ReloadTriggerEvents();
            }
        }

        public void RunTaskOnNextStartup(string key)
        {
            var path = Path.Combine(ApplicationPaths.CachePath, "startuptasks.txt");

            List<string> lines;

            try
            {
                lines = _fileSystem.ReadAllLines(path).ToList() ;
            }
            catch
            {
                lines = new List<string>();
            }

            if (!lines.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                lines.Add(key);
                _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(path));
                _fileSystem.WriteAllLines(path, lines);
            }
        }

        private void RunStartupTasks()
        {
            var path = Path.Combine(ApplicationPaths.CachePath, "startuptasks.txt");

            // ToDo: Fix this shit
            if (!File.Exists(path))
                return;

            List<string> lines;

            try
            {
                lines = _fileSystem.ReadAllLines(path).Where(i => !string.IsNullOrWhiteSpace(i)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                foreach (var key in lines)
                {
                    var task = ScheduledTasks.FirstOrDefault(i => string.Equals(i.ScheduledTask.Key, key, StringComparison.OrdinalIgnoreCase));

                    if (task != null)
                    {
                        QueueScheduledTask(task, new TaskOptions());
                    }
                }

                _fileSystem.DeleteFile(path);
            }
            catch
            {
                return;
            }
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
                Logger.LogError("Unable to find scheduled task of type {0} in QueueScheduledTask.", typeof(T).Name);
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
                Logger.LogError("Unable to find scheduled task of type {0} in Execute.", typeof(T).Name);
            }
            else
            {
                var type = scheduledTask.ScheduledTask.GetType();

                Logger.LogInformation("Queueing task {0}", type.Name);

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
                Logger.LogError("Unable to find scheduled task of type {0} in QueueScheduledTask.", task.GetType().Name);
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

            Logger.LogInformation("Queueing task {0}", type.Name);

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
            var myTasks = ScheduledTasks.ToList();

            var list = tasks.ToList();
            myTasks.AddRange(list.Select(t => new ScheduledTaskWorker(t, ApplicationPaths, this, JsonSerializer, Logger, _fileSystem, _systemEvents)));

            ScheduledTasks = myTasks.ToArray();

            BindToSystemEvent();

            RunStartupTasks();
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
            TaskExecuting?.Invoke(this, new GenericEventArgs<IScheduledTaskWorker>
            {
                Argument = task
            });
        }

        /// <summary>
        /// Called when [task completed].
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="result">The result.</param>
        internal void OnTaskCompleted(IScheduledTaskWorker task, TaskResult result)
        {
            TaskCompleted?.Invoke(task, new TaskCompletionEventArgs
            {
                Result = result,
                Task = task
            });

            ExecuteQueuedTasks();
        }

        /// <summary>
        /// Executes the queued tasks.
        /// </summary>
        private void ExecuteQueuedTasks()
        {
            Logger.LogInformation("ExecuteQueuedTasks");

            // Execute queued tasks
            lock (_taskQueue)
            {
                var list = new List<Tuple<Type, TaskOptions>>();

                Tuple<Type, TaskOptions> item;
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
