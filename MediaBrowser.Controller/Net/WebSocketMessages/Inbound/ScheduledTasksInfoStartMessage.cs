using System.Collections.Generic;
using System.ComponentModel;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Tasks;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Inbound;

/// <summary>
/// Scheduled tasks info start message.
/// </summary>
public class ScheduledTasksInfoStartMessage : WebSocketMessage<IReadOnlyCollection<TaskInfo>>, IInboundWebSocketMessage
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
    [DefaultValue(SessionMessageType.ScheduledTasksInfoStart)]
    public override SessionMessageType MessageType => SessionMessageType.ScheduledTasksInfoStart;
}
