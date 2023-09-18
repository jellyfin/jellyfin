using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Server shutting down message.
/// </summary>
public class ServerShuttingDownMessage : OutboundWebSocketMessage
{
    /// <inheritdoc />
    [DefaultValue(SessionMessageType.ServerShuttingDown)]
    public override SessionMessageType MessageType => SessionMessageType.ServerShuttingDown;
}
