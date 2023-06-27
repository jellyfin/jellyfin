using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Keep alive websocket messages.
/// </summary>
public class OutboundKeepAliveMessage : OutboundWebSocketMessage<int>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundKeepAliveMessage"/> class.
    /// </summary>
    /// <param name="data">The seconds to keep alive for.</param>
    public OutboundKeepAliveMessage(int data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.KeepAlive)]
    public override SessionMessageType MessageType => SessionMessageType.KeepAlive;
}
