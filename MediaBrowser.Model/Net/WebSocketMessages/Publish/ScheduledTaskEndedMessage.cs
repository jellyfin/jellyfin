using MediaBrowser.Model.Session;
using MediaBrowser.Model.Tasks;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Scheduled task ended message.
/// </summary>
public class ScheduledTaskEndedMessage : WebSocketMessage<TaskResult>
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
    public override SessionMessageType MessageType => SessionMessageType.ScheduledTaskEnded;
}
