using System.Collections.Generic;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Subscribe;

/// <summary>
/// Activity log entry start message.
/// </summary>
public class ActivityLogEntryStartMessage : WebSocketMessage<IReadOnlyCollection<ActivityLogEntry>>
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
    public override SessionMessageType MessageType => SessionMessageType.ActivityLogEntryStart;
}
