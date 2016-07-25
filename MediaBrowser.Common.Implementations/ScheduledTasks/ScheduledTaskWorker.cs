using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Common.Implementations.ScheduledTasks
{
    /// <summary>
    /// Class ScheduledTaskWorker
    /// </summary>
    public class ScheduledTaskWorker : IScheduledTaskWorker
    {
        public event EventHandler<GenericEventArgs<double>> TaskProgress;

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
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTaskWorker" /> class.
        /// </summary>
        /// <param name="scheduledTask">The scheduled task.</param>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">
        /// scheduledTask
        /// or
        /// applicationPaths
        /// or
        /// taskManager
        /// or
        /// jsonSerializer
        /// or
        /// logger
        /// </exception>
        public ScheduledTaskWorker(IScheduledTask scheduledTask, IApplicationPaths applicationPaths, ITaskManager taskManager, IJsonSerializer jsonSerializer, ILogger logger, IFileSystem fileSystem)
        {
            if (scheduledTask == null)
            {
                throw new ArgumentNullException("scheduledTask");
            }
            if (applicationPaths == null)
            {
                throw new ArgumentNullException("applicationPaths");
            }
            if (taskManager == null)
            {
                throw new ArgumentNullException("taskManager");
            }
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            ScheduledTask = scheduledTask;
            ApplicationPaths = applicationPaths;
            TaskManager = taskManager;
            JsonSerializer = jsonSerializer;
            Logger = logger;
            _fileSystem = fileSystem;

            InitTriggerEvents();
        }

        private bool _readFromFile = false;
        /// <summary>
        /// The _last execution result
        /// </summary>
        private TaskResult _lastExecutionResult;
        /// <summary>
        /// The _last execution result sync lock
        /// </summary>
        private readonly object _lastExecutionResultSyncLock = new object();
        /// <summary>
        /// Gets the last execution result.
        /// </summary>
        /// <value>The last execution result.</value>
        public TaskResult LastExecutionResult
        {
            get
            {
                var path = GetHistoryFilePath();

                lock (_lastExecutionResultSyncLock)
                {
                    if (_lastExecutionResult == null && !_readFromFile)
                    {
                        try
                        {
                            _lastExecutionResult = JsonSerializer.DeserializeFromFile<TaskResult>(path);
                        }
                        catch (DirectoryNotFoundException)
                        {
                            // File doesn't exist. No biggie
                        }
                        catch (FileNotFoundException)
                        {
                            // File doesn't exist. No biggie
                        }
                        catch (Exception ex)
                        {
                            Logger.ErrorException("Error deserializing {0}", ex, path);
                        }
                        _readFromFile = true;
                    }
                }

                return _lastExecutionResult;
            }
            private set
            {
                _lastExecutionResult = value;

                var path = GetHistoryFilePath();
				_fileSystem.CreateDirectory(Path.GetDirectoryName(path));

                lock (_lastExecutionResultSyncLock)
                {
                    JsonSerializer.SerializeToFile(value, path);
                }
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
        private List<ITaskTrigger> _triggers;
        /// <summary>
        /// Gets the triggers that define when the task will run
        /// </summary>
        /// <value>The triggers.</value>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public IEnumerable<ITaskTrigger> Triggers
        {
            get
            {
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

                ReloadTriggerEvents(false);

                SaveTriggers(_triggers);
            }
        }

        /// <summary>
        /// The _id
        /// </summary>
        private string _id;

        /// <summary>
        /// Gets the unique id.
        /// </summary>
        /// <value>The unique id.</value>
        public string Id
        {
            get
            {
                if (_id == null)
                {
                    _id = ScheduledTask.GetType().FullName.GetMD5().ToString("N");
                }

                return _id;
            }
        }

        private void InitTriggerEvents()
        {
            _triggers = LoadTriggers();
            ReloadTriggerEvents(true);
        }

        public void ReloadTriggerEvents()
        {
            ReloadTriggerEvents(false);
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
                trigger.Start(LastExecutionResult, Logger, Name, isApplicationStartup);
            }
        }

        /// <summary>
        /// Handles the Triggered event of the trigger control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        async void trigger_Triggered(object sender, GenericEventArgs<TaskExecutionOptions> e)
        {
            var trigger = (ITaskTrigger)sender;

            var configurableTask = ScheduledTask as IConfigurableScheduledTask;

            if (configurableTask != null && !configurableTask.IsEnabled)
            {
                return;
            }

            Logger.Info("{0} fired for task: {1}", trigger.GetType().Name, Name);

            trigger.Stop();

            TaskManager.QueueScheduledTask(ScheduledTask);

            await Task.Delay(1000).ConfigureAwait(false);

            trigger.Start(LastExecutionResult, Logger, Name, false);
        }

        private Task _currentTask;

        /// <summary>
        /// Executes the task
        /// </summary>
        /// <param name="options">Task options.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.InvalidOperationException">Cannot execute a Task that is already running</exception>
        public async Task Execute(TaskExecutionOptions options)
        {
            var task = ExecuteInternal(options);

            _currentTask = task;

            try
            {
                await task.ConfigureAwait(false);
            }
            finally
            {
                _currentTask = null;
            }
        }

        private async Task ExecuteInternal(TaskExecutionOptions options)
        {
            // Cancel the current execution, if any
            if (CurrentCancellationTokenSource != null)
            {
                throw new InvalidOperationException("Cannot execute a Task that is already running");
            }

            var progress = new Progress<double>();

            CurrentCancellationTokenSource = new CancellationTokenSource();

            Logger.Info("Executing {0}", Name);

            ((TaskManager)TaskManager).OnTaskExecuting(this);

            progress.ProgressChanged += progress_ProgressChanged;

            TaskCompletionStatus status;
            CurrentExecutionStartTime = DateTime.UtcNow;

            Exception failureException = null;

            try
            {
                var localTask = ScheduledTask.Execute(CurrentCancellationTokenSource.Token, progress);

                if (options != null && options.MaxRuntimeMs.HasValue)
                {
                    CurrentCancellationTokenSource.CancelAfter(options.MaxRuntimeMs.Value);
                }

                await localTask.ConfigureAwait(false);

                status = TaskCompletionStatus.Completed;
            }
            catch (OperationCanceledException)
            {
                status = TaskCompletionStatus.Cancelled;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error", ex);

                failureException = ex;

                status = TaskCompletionStatus.Failed;
            }

            var startTime = CurrentExecutionStartTime;
            var endTime = DateTime.UtcNow;

            progress.ProgressChanged -= progress_ProgressChanged;
            CurrentCancellationTokenSource.Dispose();
            CurrentCancellationTokenSource = null;
            CurrentProgress = null;

            OnTaskCompleted(startTime, endTime, status, failureException);

            // Bad practice, i know. But we keep a lot in memory, unfortunately.
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.Collect(2, GCCollectionMode.Forced, true);
        }

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        private Task ExecuteTask(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return Task.Run(async () => await ScheduledTask.Execute(cancellationToken, progress).ConfigureAwait(false), cancellationToken);
        }

        /// <summary>
        /// Progress_s the progress changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void progress_ProgressChanged(object sender, double e)
        {
            CurrentProgress = e;

            EventHelper.FireEventIfNotNull(TaskProgress, this, new GenericEventArgs<double>
            {
                Argument = e

            }, Logger);
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
        /// Gets the scheduled tasks configuration directory.
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetScheduledTasksConfigurationDirectory()
        {
            return Path.Combine(ApplicationPaths.ConfigurationDirectoryPath, "ScheduledTasks");
        }

        /// <summary>
        /// Gets the scheduled tasks data directory.
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetScheduledTasksDataDirectory()
        {
            return Path.Combine(ApplicationPaths.DataPath, "ScheduledTasks");
        }

        /// <summary>
        /// Gets the history file path.
        /// </summary>
        /// <value>The history file path.</value>
        private string GetHistoryFilePath()
        {
            return Path.Combine(GetScheduledTasksDataDirectory(), new Guid(Id) + ".js");
        }

        /// <summary>
        /// Gets the configuration file path.
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetConfigurationFilePath()
        {
            return Path.Combine(GetScheduledTasksConfigurationDirectory(), new Guid(Id) + ".js");
        }

        /// <summary>
        /// Loads the triggers.
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        private List<ITaskTrigger> LoadTriggers()
        {
            try
            {
                return JsonSerializer.DeserializeFromFile<IEnumerable<TaskTriggerInfo>>(GetConfigurationFilePath())
                .Select(ScheduledTaskHelpers.GetTrigger)
                .ToList();
            }
            catch (FileNotFoundException)
            {
                // File doesn't exist. No biggie. Return defaults.
                return ScheduledTask.GetDefaultTriggers().ToList();
            }
            catch (DirectoryNotFoundException)
            {
                // File doesn't exist. No biggie. Return defaults.
                return ScheduledTask.GetDefaultTriggers().ToList();
            }
        }

        /// <summary>
        /// Saves the triggers.
        /// </summary>
        /// <param name="triggers">The triggers.</param>
        private void SaveTriggers(IEnumerable<ITaskTrigger> triggers)
        {
            var path = GetConfigurationFilePath();

			_fileSystem.CreateDirectory(Path.GetDirectoryName(path));

            JsonSerializer.SerializeToFile(triggers.Select(ScheduledTaskHelpers.GetTriggerInfo), path);
        }

        /// <summary>
        /// Called when [task completed].
        /// </summary>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        /// <param name="status">The status.</param>
        private void OnTaskCompleted(DateTime startTime, DateTime endTime, TaskCompletionStatus status, Exception ex)
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

            var hasKey = ScheduledTask as IHasKey;
            if (hasKey != null)
            {
                result.Key = hasKey.Key;
            }

            if (ex != null)
            {
                result.ErrorMessage = ex.Message;
                result.LongErrorMessage = ex.StackTrace;
            }

            LastExecutionResult = result;

            ((TaskManager)TaskManager).OnTaskCompleted(this, result);
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

                var wassRunning = State == TaskState.Running;
                var startTime = CurrentExecutionStartTime;

                var token = CurrentCancellationTokenSource;
                if (token != null)
                {
                    try
                    {
                        Logger.Info(Name + ": Cancelling");
                        token.Cancel();
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("Error calling CancellationToken.Cancel();", ex);
                    }
                }
                var task = _currentTask;
                if (task != null)
                {
                    try
                    {
                        Logger.Info(Name + ": Waiting on Task");
                        var exited = Task.WaitAll(new[] { task }, 2000);

                        if (exited)
                        {
                            Logger.Info(Name + ": Task exited");
                        }
                        else
                        {
                            Logger.Info(Name + ": Timed out waiting for task to stop");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("Error calling Task.WaitAll();", ex);
                    }
                }

                if (token != null)
                {
                    try
                    {
                        Logger.Debug(Name + ": Disposing CancellationToken");
                        token.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("Error calling CancellationToken.Dispose();", ex);
                    }
                }
                if (wassRunning)
                {
                    OnTaskCompleted(startTime, DateTime.UtcNow, TaskCompletionStatus.Aborted, null);
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
