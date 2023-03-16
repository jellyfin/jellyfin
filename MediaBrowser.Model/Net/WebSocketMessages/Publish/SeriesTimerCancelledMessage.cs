using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Series timer cancelled message.
/// </summary>
public class SeriesTimerCancelledMessage : WebSocketMessage<TimerEventInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeriesTimerCancelledMessage"/> class.
    /// </summary>
    /// <param name="data">The timer event info.</param>
    public SeriesTimerCancelledMessage(TimerEventInfo data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.TimerCancelled;
}
