using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Inbound;

/// <summary>
/// Scheduled tasks info stop message.
/// </summary>
public class ScheduledTasksInfoStopMessage : InboundWebSocketMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledTasksInfoStopMessage"/> class.
    /// </summary>
    public ScheduledTasksInfoStopMessage()
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.ScheduledTasksInfoStop)]
    public override SessionMessageType MessageType => SessionMessageType.ScheduledTasksInfoStop;
}
