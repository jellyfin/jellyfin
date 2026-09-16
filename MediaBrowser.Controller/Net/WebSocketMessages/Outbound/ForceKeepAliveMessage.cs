using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Force keep alive websocket messages. The data is the timeout in seconds after which the
/// server considers the connection lost; clients are expected to answer with a KeepAlive
/// message and to keep sending one at least every half of that timeout.
/// </summary>
public class ForceKeepAliveMessage : OutboundWebSocketMessage<int>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForceKeepAliveMessage"/> class.
    /// </summary>
    /// <param name="data">The timeout in seconds.</param>
    public ForceKeepAliveMessage(int data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.ForceKeepAlive)]
    public override SessionMessageType MessageType => SessionMessageType.ForceKeepAlive;
}
