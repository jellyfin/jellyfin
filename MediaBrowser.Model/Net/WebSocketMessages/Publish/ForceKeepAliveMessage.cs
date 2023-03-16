using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Force keep alive websocket messages.
/// </summary>
public class ForceKeepAliveMessage : WebSocketMessage<int>
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
    public override SessionMessageType MessageType => SessionMessageType.ForceKeepAlive;
}
