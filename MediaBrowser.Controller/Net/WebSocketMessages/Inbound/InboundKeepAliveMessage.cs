using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Inbound;

/// <summary>
/// Keep alive websocket messages.
/// </summary>
public class InboundKeepAliveMessage : InboundWebSocketMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InboundKeepAliveMessage"/> class.
    /// </summary>
    public InboundKeepAliveMessage()
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.KeepAlive)]
    public override SessionMessageType MessageType => SessionMessageType.KeepAlive;
}
