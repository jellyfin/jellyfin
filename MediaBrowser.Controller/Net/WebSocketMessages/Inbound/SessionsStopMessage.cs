using System.ComponentModel;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Inbound;

/// <summary>
/// Sessions stop message.
/// </summary>
public class SessionsStopMessage : WebSocketMessage<SessionInfo>, IInboundWebSocketMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionsStopMessage"/> class.
    /// </summary>
    /// <param name="data">Session info.</param>
    public SessionsStopMessage(SessionInfo data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.SessionsStop)]
    public override SessionMessageType MessageType => SessionMessageType.SessionsStop;
}
