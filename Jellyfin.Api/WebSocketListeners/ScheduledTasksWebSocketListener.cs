using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.WebSocketListeners;

/// <summary>
/// Class ScheduledTasksWebSocketListener.
/// </summary>
public class ScheduledTasksWebSocketListener : BasePeriodicWebSocketListener<IEnumerable<TaskInfo>, WebSocketListenerState>
{
    /// <summary>
    /// Gets or sets the task manager.
    /// </summary>
    /// <value>The task manager.</value>
    private readonly ITaskManager _taskManager;

    private bool _disposed;

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
        _taskManager.TaskCompleted += OnTaskCompleted;
    }

    /// <inheritdoc />
    protected override SessionMessageType Type => SessionMessageType.ScheduledTasksInfo;

    /// <inheritdoc />
    protected override SessionMessageType StartType => SessionMessageType.ScheduledTasksInfoStart;

    /// <inheritdoc />
    protected override SessionMessageType StopType => SessionMessageType.ScheduledTasksInfoStop;

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
    protected override async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            _taskManager.TaskExecuting -= OnTaskExecuting;
            _taskManager.TaskCompleted -= OnTaskCompleted;
            _disposed = true;
        }

        await base.DisposeAsyncCore().ConfigureAwait(false);
    }

    private void OnTaskCompleted(object? sender, TaskCompletionEventArgs e)
    {
        e.Task.TaskProgress -= OnTaskProgress;
        SendData(true);
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
