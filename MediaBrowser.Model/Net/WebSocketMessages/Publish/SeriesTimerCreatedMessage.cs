using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Series timer created message.
/// </summary>
public class SeriesTimerCreatedMessage : WebSocketMessage<TimerEventInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeriesTimerCreatedMessage"/> class.
    /// </summary>
    /// <param name="data">timer event info.</param>
    public SeriesTimerCreatedMessage(TimerEventInfo data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.SeriesTimerCreated;
}
