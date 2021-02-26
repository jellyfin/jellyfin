using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Server.Implementations.Events.Consumers.System
{
    /// <summary>
    /// Notifies admin users when a task is completed.
    /// </summary>
    public sealed class TaskCompletedNotifier : IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        private readonly ITaskManager _taskManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCompletedNotifier"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="taskManager">The task manager.</param>
        public TaskCompletedNotifier(ISessionManager sessionManager, ITaskManager taskManager)
        {
            _sessionManager = sessionManager;
            _taskManager = taskManager;
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            _taskManager.TaskCompleted += Handle;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _taskManager.TaskCompleted -= Handle;
        }

        private async void Handle(object? sender, TaskCompletionEventArgs eventArgs)
        {
            await _sessionManager.SendMessageToAdminSessions(SessionMessageType.ScheduledTaskEnded, eventArgs.Result, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
