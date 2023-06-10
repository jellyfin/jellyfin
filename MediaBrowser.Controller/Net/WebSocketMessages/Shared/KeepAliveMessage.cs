using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Shared;

/// <summary>
/// Keep alive websocket messages.
/// </summary>
public class KeepAliveMessage : WebSocketMessage<int>, IInboundWebSocketMessage, IOutboundWebSocketMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeepAliveMessage"/> class.
    /// </summary>
    /// <param name="data">The seconds to keep alive for.</param>
    public KeepAliveMessage(int data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.KeepAlive)]
    public override SessionMessageType MessageType => SessionMessageType.KeepAlive;
}
