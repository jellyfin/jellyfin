#pragma warning disable SA1649 // File name must equal class name.

namespace MediaBrowser.Model.Net;

/// <summary>
/// Class WebSocketMessage.
/// TODO make this abstract, remove empty ctor.
/// </summary>
/// <typeparam name="T">The type of the data.</typeparam>
public class WebSocketMessage<T> : WebSocketMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketMessage{T}"/> class.
    /// </summary>
    public WebSocketMessage()
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
    /// TODO make this set only.
    /// </summary>
    public T? Data { get; set; }
}
