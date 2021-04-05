using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;

namespace Jellyfin.Api.WebSocketListeners
{
    /// <summary>
    /// Class ScheduledTasksWebSocketListener.
    /// </summary>
    public class ScheduledTasksWebSocketListener : BasePeriodicWebSocketListener<IEnumerable<TaskInfo>, WebSocketListenerState>,
        IHandleMessages<TaskCompletionEventArgs>
    {
        /// <summary>
        /// Gets or sets the task manager.
        /// </summary>
        /// <value>The task manager.</value>
        private readonly ITaskManager _taskManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTasksWebSocketListener"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{ScheduledTasksWebSocketListener}"/> interface.</param>
        /// <param name="taskManager">Instance of the <see cref="ITaskManager"/> interface.</param>
        public ScheduledTasksWebSocketListener(ILogger<ScheduledTasksWebSocketListener> logger, ITaskManager taskManager)
            : base(logger)
        {
            _taskManager = taskManager;

            _taskManager.TaskExecuting += OnTaskExecuting;
        }

        /// <inheritdoc />
        protected override SessionMessageType Type => SessionMessageType.ScheduledTasksInfo;

        /// <inheritdoc />
        protected override SessionMessageType StartType => SessionMessageType.ScheduledTasksInfoStart;

        /// <inheritdoc />
        protected override SessionMessageType StopType => SessionMessageType.ScheduledTasksInfoStop;

        /// <inheritdoc />
        public async Task Handle(TaskCompletionEventArgs e)
        {
            await SendData(true).ConfigureAwait(false);
            e.Task.TaskProgress -= OnTaskProgress;
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <returns>Task{IEnumerable{TaskInfo}}.</returns>
        protected override Task<IEnumerable<TaskInfo>> GetDataToSend()
        {
            return Task.FromResult(_taskManager.ScheduledTasks
                .OrderBy(i => i.Name)
                .Select(ScheduledTaskHelpers.GetTaskInfo)
                .Where(i => !i.IsHidden));
        }

        /// <inheritdoc />
        protected override void Dispose(bool dispose)
        {
            _taskManager.TaskExecuting -= OnTaskExecuting;

            base.Dispose(dispose);
        }

        private void OnTaskExecuting(object? sender, GenericEventArgs<IScheduledTaskWorker> e)
        {
            SendData(true);
            e.Argument.TaskProgress += OnTaskProgress;
        }

        private void OnTaskProgress(object? sender, GenericEventArgs<double> e)
        {
            SendData(false);
        }
    }
}
