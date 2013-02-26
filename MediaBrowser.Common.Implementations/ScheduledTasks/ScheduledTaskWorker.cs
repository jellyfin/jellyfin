using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Implementations.ScheduledTasks
{
    /// <summary>
    /// Class ScheduledTaskWorker
    /// </summary>
    public class ScheduledTaskWorker : IScheduledTaskWorker
    {
        /// <summary>
        /// Gets or sets the scheduled task.
        /// </summary>
        /// <value>The scheduled task.</value>
        public IScheduledTask ScheduledTask { get; private set; }

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
        /// Gets the task manager.
        /// </summary>
        /// <value>The task manager.</value>
        private ITaskManager TaskManager { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTaskWorker" /> class.
        /// </summary>
        /// <param name="scheduledTask">The scheduled task.</param>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        public ScheduledTaskWorker(IScheduledTask scheduledTask, IApplicationPaths applicationPaths, ITaskManager taskManager, IJsonSerializer jsonSerializer, ILogger logger)
        {
            ScheduledTask = scheduledTask;
            ApplicationPaths = applicationPaths;
            TaskManager = taskManager;
            JsonSerializer = jsonSerializer;
            Logger = logger;
        }

        /// <summary>
        /// The _last execution result
        /// </summary>
        private TaskResult _lastExecutionResult;
        /// <summary>
        /// The _last execution resultinitialized
        /// </summary>
        private bool _lastExecutionResultinitialized;
        /// <summary>
        /// The _last execution result sync lock
        /// </summary>
        private object _lastExecutionResultSyncLock = new object();
        /// <summary>
        /// Gets the last execution result.
        /// </summary>
        /// <value>The last execution result.</value>
        public TaskResult LastExecutionResult
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _lastExecutionResult, ref _lastExecutionResultinitialized, ref _lastExecutionResultSyncLock, () =>
                {
                    try
                    {
                        return JsonSerializer.DeserializeFromFile<TaskResult>(GetHistoryFilePath());
                    }
                    catch (IOException)
                    {
                        // File doesn't exist. No biggie
                        return null;
                    }
                });

                return _lastExecutionResult;
            }
            private set
            {
                _lastExecutionResult = value;

                _lastExecutionResultinitialized = value != null;
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return ScheduledTask.Name; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return ScheduledTask.Description; }
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public string Category
        {
            get { return ScheduledTask.Category; }
        }

        /// <summary>
        /// Gets the current cancellation token
        /// </summary>
        /// <value>The current cancellation token source.</value>
        private CancellationTokenSource CurrentCancellationTokenSource { get; set; }

        /// <summary>
        /// Gets or sets the current execution start time.
        /// </summary>
        /// <value>The current execution start time.</value>
        private DateTime CurrentExecutionStartTime { get; set; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>The state.</value>
        public TaskState State
        {
            get
            {
                if (CurrentCancellationTokenSource != null)
                {
                    return CurrentCancellationTokenSource.IsCancellationRequested
                               ? TaskState.Cancelling
                               : TaskState.Running;
                }

                return TaskState.Idle;
            }
        }

        /// <summary>
        /// Gets the current progress.
        /// </summary>
        /// <value>The current progress.</value>
        public double? CurrentProgress { get; private set; }

        /// <summary>
        /// The _triggers
        /// </summary>
        private IEnumerable<ITaskTrigger> _triggers;
        /// <summary>
        /// The _triggers initialized
        /// </summary>
        private bool _triggersInitialized;
        /// <summary>
        /// The _triggers sync lock
        /// </summary>
        private object _triggersSyncLock = new object();
        /// <summary>
        /// Gets the triggers that define when the task will run
        /// </summary>
        /// <value>The triggers.</value>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public IEnumerable<ITaskTrigger> Triggers
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _triggers, ref _triggersInitialized, ref _triggersSyncLock, () => LoadTriggers());

                return _triggers;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                // Cleanup current triggers
                if (_triggers != null)
                {
                    DisposeTriggers();
                }

                _triggers = value.ToList();

                _triggersInitialized = true;

                ReloadTriggerEvents(false);

                SaveTriggers(_triggers);
            }
        }

        /// <summary>
        /// The _id
        /// </summary>
        private Guid? _id;

        /// <summary>
        /// Gets the unique id.
        /// </summary>
        /// <value>The unique id.</value>
        public Guid Id
        {
            get
            {
                if (!_id.HasValue)
                {
                    _id = ScheduledTask.GetType().FullName.GetMD5();
                }

                return _id.Value;
            }
        }

        /// <summary>
        /// Reloads the trigger events.
        /// </summary>
        /// <param name="isApplicationStartup">if set to <c>true</c> [is application startup].</param>
        private void ReloadTriggerEvents(bool isApplicationStartup)
        {
            foreach (var trigger in Triggers)
            {
                trigger.Stop();

                trigger.Triggered -= trigger_Triggered;
                trigger.Triggered += trigger_Triggered;
                trigger.Start(isApplicationStartup);
            }
        }

        /// <summary>
        /// Handles the Triggered event of the trigger control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        async void trigger_Triggered(object sender, EventArgs e)
        {
            var trigger = (ITaskTrigger)sender;

            Logger.Info("{0} fired for task: {1}", trigger.GetType().Name, Name);

            trigger.Stop();

            TaskManager.QueueScheduledTask(ScheduledTask);

            await Task.Delay(1000).ConfigureAwait(false);

            trigger.Start(false);
        }

        /// <summary>
        /// Executes the task
        /// </summary>
        /// <returns>Task.</returns>
        /// <exception cref="System.InvalidOperationException">Cannot execute a Task that is already running</exception>
        public async Task Execute()
        {
            // Cancel the current execution, if any
            if (CurrentCancellationTokenSource != null)
            {
                throw new InvalidOperationException("Cannot execute a Task that is already running");
            }

            CurrentCancellationTokenSource = new CancellationTokenSource();

            Logger.Info("Executing {0}", Name);

            var progress = new Progress<double>();

            progress.ProgressChanged += progress_ProgressChanged;

            TaskCompletionStatus status;
            CurrentExecutionStartTime = DateTime.UtcNow;

            //Kernel.TcpManager.SendWebSocketMessage("ScheduledTaskBeginExecute", Name);

            try
            {
                await System.Threading.Tasks.Task.Run(async () => await ScheduledTask.Execute(CurrentCancellationTokenSource.Token, progress).ConfigureAwait(false)).ConfigureAwait(false);

                status = TaskCompletionStatus.Completed;
            }
            catch (OperationCanceledException)
            {
                status = TaskCompletionStatus.Cancelled;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error", ex);

                status = TaskCompletionStatus.Failed;
            }

            var startTime = CurrentExecutionStartTime;
            var endTime = DateTime.UtcNow;

            //Kernel.TcpManager.SendWebSocketMessage("ScheduledTaskEndExecute", LastExecutionResult);

            progress.ProgressChanged -= progress_ProgressChanged;
            CurrentCancellationTokenSource.Dispose();
            CurrentCancellationTokenSource = null;
            CurrentProgress = null;

            OnTaskCompleted(startTime, endTime, status);
        }

        /// <summary>
        /// Progress_s the progress changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void progress_ProgressChanged(object sender, double e)
        {
            CurrentProgress = e;
        }

        /// <summary>
        /// Stops the task if it is currently executing
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Cannot cancel a Task unless it is in the Running state.</exception>
        public void Cancel()
        {
            if (State != TaskState.Running)
            {
                throw new InvalidOperationException("Cannot cancel a Task unless it is in the Running state.");
            }

            CancelIfRunning();
        }

        /// <summary>
        /// Cancels if running.
        /// </summary>
        public void CancelIfRunning()
        {
            if (State == TaskState.Running)
            {
                Logger.Info("Attempting to cancel Scheduled Task {0}", Name);
                CurrentCancellationTokenSource.Cancel();
            }
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
                    _scheduledTasksConfigurationDirectory = Path.Combine(ApplicationPaths.ConfigurationDirectoryPath, "ScheduledTasks");

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
                    _scheduledTasksDataDirectory = Path.Combine(ApplicationPaths.DataPath, "ScheduledTasks");

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
        private string GetHistoryFilePath()
        {
            return Path.Combine(ScheduledTasksDataDirectory, Id + ".js");
        }

        /// <summary>
        /// Gets the configuration file path.
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetConfigurationFilePath()
        {
            return Path.Combine(ScheduledTasksConfigurationDirectory, Id + ".js");
        }

        /// <summary>
        /// Loads the triggers.
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        private IEnumerable<ITaskTrigger> LoadTriggers()
        {
            try
            {
                return JsonSerializer.DeserializeFromFile<IEnumerable<TaskTriggerInfo>>(GetConfigurationFilePath())
                .Select(ScheduledTaskHelpers.GetTrigger)
                .ToList();
            }
            catch (IOException)
            {
                // File doesn't exist. No biggie. Return defaults.
                return ScheduledTask.GetDefaultTriggers();
            }
        }

        /// <summary>
        /// Saves the triggers.
        /// </summary>
        /// <param name="triggers">The triggers.</param>
        private void SaveTriggers(IEnumerable<ITaskTrigger> triggers)
        {
            JsonSerializer.SerializeToFile(triggers.Select(ScheduledTaskHelpers.GetTriggerInfo), GetConfigurationFilePath());
        }

        /// <summary>
        /// Called when [task completed].
        /// </summary>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        /// <param name="status">The status.</param>
        private void OnTaskCompleted(DateTime startTime, DateTime endTime, TaskCompletionStatus status)
        {
            var elapsedTime = endTime - startTime;

            Logger.Info("{0} {1} after {2} minute(s) and {3} seconds", Name, status, Math.Truncate(elapsedTime.TotalMinutes), elapsedTime.Seconds);

            var result = new TaskResult
            {
                StartTimeUtc = startTime,
                EndTimeUtc = endTime,
                Status = status,
                Name = Name,
                Id = Id
            };

            JsonSerializer.SerializeToFile(result, GetHistoryFilePath());

            LastExecutionResult = result;
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
            if (dispose)
            {
                DisposeTriggers();

                if (State == TaskState.Running)
                {
                    OnTaskCompleted(CurrentExecutionStartTime, DateTime.UtcNow, TaskCompletionStatus.Aborted);
                }

                if (CurrentCancellationTokenSource != null)
                {
                    CurrentCancellationTokenSource.Dispose();
                }
            }
        }

        /// <summary>
        /// Disposes each trigger
        /// </summary>
        private void DisposeTriggers()
        {
            foreach (var trigger in Triggers)
            {
                trigger.Triggered -= trigger_Triggered;
                trigger.Stop();
            }
        }
    }
}
