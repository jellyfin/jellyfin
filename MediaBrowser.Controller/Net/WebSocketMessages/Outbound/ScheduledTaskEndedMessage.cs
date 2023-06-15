using System.ComponentModel;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Tasks;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Scheduled task ended message.
/// </summary>
public class ScheduledTaskEndedMessage : WebSocketMessage<TaskResult>, IOutboundWebSocketMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledTaskEndedMessage"/> class.
    /// </summary>
    /// <param name="data">Task result.</param>
    public ScheduledTaskEndedMessage(TaskResult data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.ScheduledTaskEnded)]
    public override SessionMessageType MessageType => SessionMessageType.ScheduledTaskEnded;
}
