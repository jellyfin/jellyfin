using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

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
        protected override string Name => "ScheduledTasksInfo";

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTasksWebSocketListener" /> class.
        /// </summary>
        public ScheduledTasksWebSocketListener(ILogger<ScheduledTasksWebSocketListener> logger, ITaskManager taskManager)
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
        /// <returns>Task{IEnumerable{TaskInfo}}.</returns>
        protected override Task<IEnumerable<TaskInfo>> GetDataToSend()
        {
            return Task.FromResult(TaskManager.ScheduledTasks
                .OrderBy(i => i.Name)
                .Select(ScheduledTaskHelpers.GetTaskInfo)
                .Where(i => !i.IsHidden));
        }

        protected override void Dispose(bool dispose)
        {
            TaskManager.TaskExecuting -= TaskManager_TaskExecuting;
            TaskManager.TaskCompleted -= TaskManager_TaskCompleted;

            base.Dispose(dispose);
        }
    }
}
