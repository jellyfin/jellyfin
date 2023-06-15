using System.Collections.Generic;
using System.ComponentModel;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Inbound;

/// <summary>
/// Activity log entry stop message.
/// </summary>
public class ActivityLogEntryStopMessage : WebSocketMessage<IReadOnlyCollection<ActivityLogEntry>>, IInboundWebSocketMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityLogEntryStopMessage"/> class.
    /// </summary>
    /// <param name="data">Collection of activity log entries.</param>
    public ActivityLogEntryStopMessage(IReadOnlyCollection<ActivityLogEntry> data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.ActivityLogEntryStop)]
    public override SessionMessageType MessageType => SessionMessageType.ActivityLogEntryStop;
}
