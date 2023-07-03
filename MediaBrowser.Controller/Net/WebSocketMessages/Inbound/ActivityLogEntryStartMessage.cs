using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Inbound;

/// <summary>
/// Activity log entry start message.
/// Data is the timing data encoded as "$initialDelay,$interval" in ms.
/// </summary>
public class ActivityLogEntryStartMessage : InboundWebSocketMessage<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityLogEntryStartMessage"/> class.
    /// Data is the timing data encoded as "$initialDelay,$interval" in ms.
    /// </summary>
    /// <param name="data">The timing data encoded as "$initialDelay,$interval".</param>
    public ActivityLogEntryStartMessage(string data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.ActivityLogEntryStart)]
    public override SessionMessageType MessageType => SessionMessageType.ActivityLogEntryStart;
}
