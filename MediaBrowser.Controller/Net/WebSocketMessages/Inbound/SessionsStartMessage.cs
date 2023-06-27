using System.ComponentModel;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Inbound;

/// <summary>
/// Sessions start message.
/// </summary>
public class SessionsStartMessage : InboundWebSocketMessage<SessionInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionsStartMessage"/> class.
    /// </summary>
    /// <param name="data">Session info.</param>
    public SessionsStartMessage(SessionInfo data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.SessionsStart)]
    public override SessionMessageType MessageType => SessionMessageType.SessionsStart;
}
