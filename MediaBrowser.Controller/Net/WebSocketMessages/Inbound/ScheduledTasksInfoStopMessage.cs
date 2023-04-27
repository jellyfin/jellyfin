using System.Collections.Generic;
using System.ComponentModel;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Tasks;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Inbound;

/// <summary>
/// Scheduled tasks info stop message.
/// </summary>
public class ScheduledTasksInfoStopMessage : WebSocketMessage<IReadOnlyCollection<TaskInfo>>, IInboundWebSocketMessage
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
    [DefaultValue(SessionMessageType.ScheduledTasksInfoStop)]
    public override SessionMessageType MessageType => SessionMessageType.ScheduledTasksInfoStop;
}
