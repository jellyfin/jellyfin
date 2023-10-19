using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Server restarting down message.
/// </summary>
public class ServerRestartingMessage : OutboundWebSocketMessage
{
    /// <inheritdoc />
    [DefaultValue(SessionMessageType.ServerRestarting)]
    public override SessionMessageType MessageType => SessionMessageType.ServerRestarting;
}
