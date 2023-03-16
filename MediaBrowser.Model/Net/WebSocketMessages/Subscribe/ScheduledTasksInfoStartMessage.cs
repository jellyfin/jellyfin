using System.Collections.Generic;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Tasks;

namespace MediaBrowser.Model.Net.WebSocketMessages.Subscribe;

/// <summary>
/// Scheduled tasks info start message.
/// </summary>
public class ScheduledTasksInfoStartMessage : WebSocketMessage<IReadOnlyCollection<TaskInfo>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledTasksInfoStartMessage"/> class.
    /// </summary>
    /// <param name="data">Collection of task info.</param>
    public ScheduledTasksInfoStartMessage(IReadOnlyCollection<TaskInfo> data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.ScheduledTasksInfoStart;
}
