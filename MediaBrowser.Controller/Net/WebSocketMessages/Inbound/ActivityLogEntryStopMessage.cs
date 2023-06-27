using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Inbound;

/// <summary>
/// Activity log entry stop message.
/// </summary>
public class ActivityLogEntryStopMessage : InboundWebSocketMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityLogEntryStopMessage"/> class.
    /// </summary>
    public ActivityLogEntryStopMessage()
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.ActivityLogEntryStop)]
    public override SessionMessageType MessageType => SessionMessageType.ActivityLogEntryStop;
}
