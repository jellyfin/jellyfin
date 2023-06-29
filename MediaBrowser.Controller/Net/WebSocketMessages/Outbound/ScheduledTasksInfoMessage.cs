using System.Collections.Generic;
using System.ComponentModel;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Tasks;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Scheduled tasks info message.
/// </summary>
public class ScheduledTasksInfoMessage : OutboundWebSocketMessage<IReadOnlyList<TaskInfo>>
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
    [DefaultValue(SessionMessageType.ScheduledTasksInfo)]
    public override SessionMessageType MessageType => SessionMessageType.ScheduledTasksInfo;
}
