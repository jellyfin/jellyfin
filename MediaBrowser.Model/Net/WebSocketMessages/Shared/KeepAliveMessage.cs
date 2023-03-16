using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Shared;

/// <summary>
/// Keep alive websocket messages.
/// </summary>
public class KeepAliveMessage : WebSocketMessage<int>
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
    public override SessionMessageType MessageType => SessionMessageType.KeepAlive;
}
