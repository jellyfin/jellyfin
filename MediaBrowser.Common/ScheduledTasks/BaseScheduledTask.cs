using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.ScheduledTasks
{
    /// <summary>
    /// Represents a task that can be executed at a scheduled time
    /// </summary>
    /// <typeparam name="TKernelType">The type of the T kernel type.</typeparam>
    public abstract class BaseScheduledTask<TKernelType> : IScheduledTask
        where TKernelType : class, IKernel
    {
        /// <summary>
        /// Gets the kernel.
        /// </summary>
        /// <value>The kernel.</value>
        protected TKernelType Kernel { get; private set; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Gets the task manager.
        /// </summary>
        /// <value>The task manager.</value>
        protected ITaskManager TaskManager { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseScheduledTask{TKernelType}" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">kernel</exception>
        protected BaseScheduledTask(TKernelType kernel, ITaskManager taskManager, ILogger logger)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException("kernel");
            }
            if (taskManager == null)
            {
                throw new ArgumentNullException("taskManager");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            Kernel = kernel;
            TaskManager = taskManager;
            Logger = logger;

            ReloadTriggerEvents(true);
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
                        return TaskManager.GetLastExecutionResult(this);
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
                LazyInitializer.EnsureInitialized(ref _triggers, ref _triggersInitialized, ref _triggersSyncLock, () => TaskManager.LoadTriggers(this));

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

                TaskManager.SaveTriggers(this, _triggers);
            }
        }

        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public abstract IEnumerable<ITaskTrigger> GetDefaultTriggers();

        /// <summary>
        /// Returns the task to be executed
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        protected abstract Task ExecuteInternal(CancellationToken cancellationToken, IProgress<double> progress);

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public abstract string Description { get; }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public virtual string Category
        {
            get { return "Application"; }
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
                    _id = GetType().FullName.GetMD5();
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

            TaskManager.QueueScheduledTask(this);

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

            Kernel.TcpManager.SendWebSocketMessage("ScheduledTaskBeginExecute", Name);

            try
            {
                await Task.Run(async () => await ExecuteInternal(CurrentCancellationTokenSource.Token, progress).ConfigureAwait(false)).ConfigureAwait(false);

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

            Kernel.TcpManager.SendWebSocketMessage("ScheduledTaskEndExecute", LastExecutionResult);

            progress.ProgressChanged -= progress_ProgressChanged;
            CurrentCancellationTokenSource.Dispose();
            CurrentCancellationTokenSource = null;
            CurrentProgress = null;

            TaskManager.OnTaskCompleted(this, startTime, endTime, status);
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
                    TaskManager.OnTaskCompleted(this, CurrentExecutionStartTime, DateTime.UtcNow, TaskCompletionStatus.Aborted);
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
