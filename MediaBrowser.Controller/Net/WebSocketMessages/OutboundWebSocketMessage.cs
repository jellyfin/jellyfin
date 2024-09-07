using System;

namespace MediaBrowser.Controller.Net.WebSocketMessages;

/// <summary>
/// Outbound websocket message.
/// </summary>
public class OutboundWebSocketMessage : WebSocketMessage, IOutboundWebSocketMessage
{
    /// <summary>
    /// Gets or sets the message id.
    /// </summary>
    public Guid MessageId { get; set; } = Guid.NewGuid();
}
