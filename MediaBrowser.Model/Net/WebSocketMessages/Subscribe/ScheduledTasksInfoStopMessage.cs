using System.Collections.Generic;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Tasks;

namespace MediaBrowser.Model.Net.WebSocketMessages.Subscribe;

/// <summary>
/// Scheduled tasks info stop message.
/// </summary>
public class ScheduledTasksInfoStopMessage : WebSocketMessage<IReadOnlyCollection<TaskInfo>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledTasksInfoStopMessage"/> class.
    /// </summary>
    /// <param name="data">Collection of task info.</param>
    public ScheduledTasksInfoStopMessage(IReadOnlyCollection<TaskInfo> data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.ScheduledTasksInfoStop;
}
