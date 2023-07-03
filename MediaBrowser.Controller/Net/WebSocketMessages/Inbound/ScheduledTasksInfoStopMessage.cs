using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Inbound;

/// <summary>
/// Scheduled tasks info stop message.
/// </summary>
public class ScheduledTasksInfoStopMessage : InboundWebSocketMessage
{
    /// <inheritdoc />
    [DefaultValue(SessionMessageType.ScheduledTasksInfoStop)]
    public override SessionMessageType MessageType => SessionMessageType.ScheduledTasksInfoStop;
}
