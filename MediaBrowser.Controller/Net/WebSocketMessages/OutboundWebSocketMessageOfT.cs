#pragma warning disable SA1649 // File name must equal class name.

using System;

namespace MediaBrowser.Controller.Net.WebSocketMessages;

/// <summary>
/// Outbound websocket message with data.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
public class OutboundWebSocketMessage<T> : WebSocketMessage<T>, IOutboundWebSocketMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundWebSocketMessage{T}"/> class.
    /// </summary>
    public OutboundWebSocketMessage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundWebSocketMessage{T}"/> class.
    /// </summary>
    /// <param name="data">The data to send.</param>
    protected OutboundWebSocketMessage(T data)
    {
        Data = data;
    }

    /// <summary>
    /// Gets or sets the message id.
    /// </summary>
    public Guid MessageId { get; set; } = Guid.NewGuid();
}
