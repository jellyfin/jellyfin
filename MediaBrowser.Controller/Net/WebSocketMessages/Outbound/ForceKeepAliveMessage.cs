using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Force keep alive websocket messages.
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
