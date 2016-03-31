using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.ScheduledTasks
{
    /// <summary>
    /// Class ScheduledTasksWebSocketListener
    /// </summary>
    public class ScheduledTasksWebSocketListener : BasePeriodicWebSocketListener<IEnumerable<TaskInfo>, WebSocketListenerState>
    {
        /// <summary>
        /// Gets or sets the task manager.
        /// </summary>
        /// <value>The task manager.</value>
        private ITaskManager TaskManager { get; set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        protected override string Name
        {
            get { return "ScheduledTasksInfo"; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTasksWebSocketListener" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="taskManager">The task manager.</param>
        public ScheduledTasksWebSocketListener(ILogger logger, ITaskManager taskManager)
            : base(logger)
        {
            TaskManager = taskManager;

            TaskManager.TaskExecuting += TaskManager_TaskExecuting;
            TaskManager.TaskCompleted += TaskManager_TaskCompleted;
        }

        void TaskManager_TaskCompleted(object sender, TaskCompletionEventArgs e)
        {
            SendData(true);
            e.Task.TaskProgress -= Argument_TaskProgress;
        }

        void TaskManager_TaskExecuting(object sender, GenericEventArgs<IScheduledTaskWorker> e)
        {
            SendData(true);
            e.Argument.TaskProgress += Argument_TaskProgress;
        }

        void Argument_TaskProgress(object sender, GenericEventArgs<double> e)
        {
            SendData(false);
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>Task{IEnumerable{TaskInfo}}.</returns>
        protected override Task<IEnumerable<TaskInfo>> GetDataToSend(WebSocketListenerState state)
        {
            return Task.FromResult(TaskManager.ScheduledTasks
                .OrderBy(i => i.Name)
                .Select(ScheduledTaskHelpers.GetTaskInfo)
                .Where(i => !i.IsHidden));
        }

        protected override bool SendOnTimer
        {
            get
            {
                return false;
            }
        }

        protected override void Dispose(bool dispose)
        {
            TaskManager.TaskExecuting -= TaskManager_TaskExecuting;
            TaskManager.TaskCompleted -= TaskManager_TaskCompleted;
            
            base.Dispose(dispose);
        }
    }
}
