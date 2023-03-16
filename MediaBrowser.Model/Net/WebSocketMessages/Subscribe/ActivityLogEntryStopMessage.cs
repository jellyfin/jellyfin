using System.Collections.Generic;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Subscribe;

/// <summary>
/// Activity log entry stop message.
/// </summary>
public class ActivityLogEntryStopMessage : WebSocketMessage<IReadOnlyCollection<ActivityLogEntry>>
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
    public override SessionMessageType MessageType => SessionMessageType.ActivityLogEntryStop;
}
