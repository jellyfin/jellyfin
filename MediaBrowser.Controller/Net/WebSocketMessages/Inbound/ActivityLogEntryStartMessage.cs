using System.Collections.Generic;
using System.ComponentModel;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Inbound;

/// <summary>
/// Activity log entry start message.
/// </summary>
public class ActivityLogEntryStartMessage : InboundWebSocketMessage<IReadOnlyCollection<ActivityLogEntry>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityLogEntryStartMessage"/> class.
    /// </summary>
    /// <param name="data">Collection of activity log entries.</param>
    public ActivityLogEntryStartMessage(IReadOnlyCollection<ActivityLogEntry> data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.ActivityLogEntryStart)]
    public override SessionMessageType MessageType => SessionMessageType.ActivityLogEntryStart;
}
