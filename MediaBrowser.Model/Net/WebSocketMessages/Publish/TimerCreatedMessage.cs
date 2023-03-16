using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Timer created message.
/// </summary>
public class TimerCreatedMessage : WebSocketMessage<TimerEventInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimerCreatedMessage"/> class.
    /// </summary>
    /// <param name="data">Timer event info.</param>
    public TimerCreatedMessage(TimerEventInfo data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.TimerCreated;
}
