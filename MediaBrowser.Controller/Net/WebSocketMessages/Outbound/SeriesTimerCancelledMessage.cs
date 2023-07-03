using System.ComponentModel;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Series timer cancelled message.
/// </summary>
public class SeriesTimerCancelledMessage : OutboundWebSocketMessage<TimerEventInfo>
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
    [DefaultValue(SessionMessageType.SeriesTimerCancelled)]
    public override SessionMessageType MessageType => SessionMessageType.SeriesTimerCancelled;
}
