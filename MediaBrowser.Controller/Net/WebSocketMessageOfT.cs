#pragma warning disable SA1649 // File name must equal class name.

namespace MediaBrowser.Controller.Net;

/// <summary>
/// Class WebSocketMessage.
/// </summary>
/// <typeparam name="T">The type of the data.</typeparam>
public abstract class WebSocketMessage<T> : WebSocketMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketMessage{T}"/> class.
    /// </summary>
    protected WebSocketMessage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketMessage{T}"/> class.
    /// </summary>
    /// <param name="data">The data to send.</param>
    protected WebSocketMessage(T data)
    {
        Data = data;
    }

    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    // TODO make this set only.
    public T? Data { get; set; }
}
