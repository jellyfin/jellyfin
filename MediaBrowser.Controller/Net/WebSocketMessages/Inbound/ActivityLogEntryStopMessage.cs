using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Inbound;

/// <summary>
/// Activity log entry stop message.
/// </summary>
public class ActivityLogEntryStopMessage : InboundWebSocketMessage
{
    /// <inheritdoc />
    [DefaultValue(SessionMessageType.ActivityLogEntryStop)]
    public override SessionMessageType MessageType => SessionMessageType.ActivityLogEntryStop;
}
