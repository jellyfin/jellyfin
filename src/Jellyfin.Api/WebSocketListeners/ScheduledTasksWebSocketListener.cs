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
    protected override void Dispose(bool dispose)
    {
        if (dispose)
        {
            _taskManager.TaskExecuting -= OnTaskExecuting;
            _taskManager.TaskCompleted -= OnTaskCompleted;
        }

        base.Dispose(dispose);
    }

    private async void OnTaskCompleted(object? sender, TaskCompletionEventArgs e)
    {
        e.Task.TaskProgress -= OnTaskProgress;
        await SendData(true).ConfigureAwait(false);
    }

    private async void OnTaskExecuting(object? sender, GenericEventArgs<IScheduledTaskWorker> e)
    {
        await SendData(true).ConfigureAwait(false);
        e.Argument.TaskProgress += OnTaskProgress;
    }

    private async void OnTaskProgress(object? sender, GenericEventArgs<double> e)
    {
        await SendData(false).ConfigureAwait(false);
    }
}
