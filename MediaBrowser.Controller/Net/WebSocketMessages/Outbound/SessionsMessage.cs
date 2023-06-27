using System.ComponentModel;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Sessions message.
/// </summary>
public class SessionsMessage : OutboundWebSocketMessage<SessionInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionsMessage"/> class.
    /// </summary>
    /// <param name="data">Session info.</param>
    public SessionsMessage(SessionInfo data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.Sessions)]
    public override SessionMessageType MessageType => SessionMessageType.Sessions;
}
