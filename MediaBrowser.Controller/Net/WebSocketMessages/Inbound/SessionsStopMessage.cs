using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Inbound;

/// <summary>
/// Sessions stop message.
/// </summary>
public class SessionsStopMessage : InboundWebSocketMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionsStopMessage"/> class.
    /// </summary>
    public SessionsStopMessage()
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.SessionsStop)]
    public override SessionMessageType MessageType => SessionMessageType.SessionsStop;
}
