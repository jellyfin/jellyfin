using System.Collections.Generic;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Tasks;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Scheduled tasks info message.
/// </summary>
public class ScheduledTasksInfoMessage : WebSocketMessage<IReadOnlyList<TaskInfo>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledTasksInfoMessage"/> class.
    /// </summary>
    /// <param name="data">List of task infos.</param>
    public ScheduledTasksInfoMessage(IReadOnlyList<TaskInfo> data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.ScheduledTasksInfo;
}
