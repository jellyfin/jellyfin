using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Inbound;

/// <summary>
/// Scheduled tasks info start message.
/// Data is the timing data encoded as "$initialDelay,$interval" in ms.
/// </summary>
public class ScheduledTasksInfoStartMessage : InboundWebSocketMessage<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledTasksInfoStartMessage"/> class.
    /// </summary>
    /// <param name="data">The timing data encoded as $initialDelay,$interval.</param>
    public ScheduledTasksInfoStartMessage(string data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.ScheduledTasksInfoStart)]
    public override SessionMessageType MessageType => SessionMessageType.ScheduledTasksInfoStart;
}
