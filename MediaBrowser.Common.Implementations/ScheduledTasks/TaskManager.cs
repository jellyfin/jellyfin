using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
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
        /// The _application paths
        /// </summary>
        private readonly IApplicationPaths _applicationPaths;

        /// <summary>
        /// The _json serializer
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskManager" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentException">kernel</exception>
        public TaskManager(IApplicationPaths applicationPaths, IJsonSerializer jsonSerializer, ILogger logger)
        {
            if (applicationPaths == null)
            {
                throw new ArgumentException("applicationPaths");
            }
            if (jsonSerializer == null)
            {
                throw new ArgumentException("jsonSerializer");
            }
            if (logger == null)
            {
                throw new ArgumentException("logger");
            }

            _applicationPaths = applicationPaths;
            _jsonSerializer = jsonSerializer;
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
        /// Adds the tasks.
        /// </summary>
        /// <param name="tasks">The tasks.</param>
        public void AddTasks(IEnumerable<IScheduledTask> tasks)
        {
            var myTasks = ScheduledTasks.ToList();

            myTasks.AddRange(tasks);

            ScheduledTasks = myTasks.ToArray();
        }

        /// <summary>
        /// The _scheduled tasks configuration directory
        /// </summary>
        private string _scheduledTasksConfigurationDirectory;
        /// <summary>
        /// Gets the scheduled tasks configuration directory.
        /// </summary>
        /// <value>The scheduled tasks configuration directory.</value>
        private string ScheduledTasksConfigurationDirectory
        {
            get
            {
                if (_scheduledTasksConfigurationDirectory == null)
                {
                    _scheduledTasksConfigurationDirectory = Path.Combine(_applicationPaths.ConfigurationDirectoryPath, "ScheduledTasks");

                    if (!Directory.Exists(_scheduledTasksConfigurationDirectory))
                    {
                        Directory.CreateDirectory(_scheduledTasksConfigurationDirectory);
                    }
                }
                return _scheduledTasksConfigurationDirectory;
            }
        }

        /// <summary>
        /// The _scheduled tasks data directory
        /// </summary>
        private string _scheduledTasksDataDirectory;
        /// <summary>
        /// Gets the scheduled tasks data directory.
        /// </summary>
        /// <value>The scheduled tasks data directory.</value>
        private string ScheduledTasksDataDirectory
        {
            get
            {
                if (_scheduledTasksDataDirectory == null)
                {
                    _scheduledTasksDataDirectory = Path.Combine(_applicationPaths.DataPath, "ScheduledTasks");

                    if (!Directory.Exists(_scheduledTasksDataDirectory))
                    {
                        Directory.CreateDirectory(_scheduledTasksDataDirectory);
                    }
                }
                return _scheduledTasksDataDirectory;
            }
        }

        /// <summary>
        /// Gets the history file path.
        /// </summary>
        /// <value>The history file path.</value>
        private string GetHistoryFilePath(IScheduledTask task)
        {
            return Path.Combine(ScheduledTasksDataDirectory, task.Id + ".js");
        }

        /// <summary>
        /// Gets the configuration file path.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns>System.String.</returns>
        private string GetConfigurationFilePath(IScheduledTask task)
        {
            return Path.Combine(ScheduledTasksConfigurationDirectory, task.Id + ".js");
        }

        /// <summary>
        /// Called when [task completed].
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        /// <param name="status">The status.</param>
        public void OnTaskCompleted(IScheduledTask task, DateTime startTime, DateTime endTime, TaskCompletionStatus status)
        {
            var elapsedTime = endTime - startTime;

            _logger.Info("{0} {1} after {2} minute(s) and {3} seconds", task.Name, status, Math.Truncate(elapsedTime.TotalMinutes), elapsedTime.Seconds);

            var result = new TaskResult
            {
                StartTimeUtc = startTime,
                EndTimeUtc = endTime,
                Status = status,
                Name = task.Name,
                Id = task.Id
            };

            _jsonSerializer.SerializeToFile(result, GetHistoryFilePath(task));

            //task.LastExecutionResult = result;
        }

        /// <summary>
        /// Gets the last execution result.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns>TaskResult.</returns>
        public TaskResult GetLastExecutionResult(IScheduledTask task)
        {
            return _jsonSerializer.DeserializeFromFile<TaskResult>(GetHistoryFilePath(task));
        }

        /// <summary>
        /// Loads the triggers.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<ITaskTrigger> LoadTriggers(IScheduledTask task)
        {
            try
            {
                return _jsonSerializer.DeserializeFromFile<IEnumerable<TaskTriggerInfo>>(GetConfigurationFilePath(task))
                .Select(ScheduledTaskHelpers.GetTrigger)
                .ToList();
            }
            catch (IOException)
            {
                // File doesn't exist. No biggie. Return defaults.
                return task.GetDefaultTriggers();
            }
        }

        /// <summary>
        /// Saves the triggers.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="triggers">The triggers.</param>
        public void SaveTriggers(IScheduledTask task, IEnumerable<ITaskTrigger> triggers)
        {
            _jsonSerializer.SerializeToFile(triggers.Select(ScheduledTaskHelpers.GetTriggerInfo), GetConfigurationFilePath(task));
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
