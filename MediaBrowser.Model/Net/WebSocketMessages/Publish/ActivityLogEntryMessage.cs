using System.Collections.Generic;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Activity log created message.
/// </summary>
public class ActivityLogEntryMessage : WebSocketMessage<IReadOnlyList<ActivityLogEntry>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityLogEntryMessage"/> class.
    /// </summary>
    /// <param name="data">List of activity log entries.</param>
    public ActivityLogEntryMessage(IReadOnlyList<ActivityLogEntry> data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.ActivityLogEntry;
}
