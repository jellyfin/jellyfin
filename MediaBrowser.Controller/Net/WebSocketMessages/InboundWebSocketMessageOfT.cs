#pragma warning disable SA1649 // File name must equal class name.

namespace MediaBrowser.Controller.Net.WebSocketMessages;

/// <summary>
/// Inbound websocket message with data.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
public class InboundWebSocketMessage<T> : WebSocketMessage<T>, IInboundWebSocketMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InboundWebSocketMessage{T}"/> class.
    /// </summary>
    public InboundWebSocketMessage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InboundWebSocketMessage{T}"/> class.
    /// </summary>
    /// <param name="data">The data to send.</param>
    protected InboundWebSocketMessage(T data)
    {
        Data = data;
    }
}
