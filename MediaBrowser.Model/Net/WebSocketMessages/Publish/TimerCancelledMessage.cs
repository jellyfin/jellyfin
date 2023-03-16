using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Timer cancelled message.
/// </summary>
public class TimerCancelledMessage : WebSocketMessage<TimerEventInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimerCancelledMessage"/> class.
    /// </summary>
    /// <param name="data">Timer event info.</param>
    public TimerCancelledMessage(TimerEventInfo data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.TimerCancelled;
}
