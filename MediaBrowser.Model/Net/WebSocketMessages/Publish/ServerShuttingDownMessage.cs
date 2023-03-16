using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Server shutting down message.
/// </summary>
public class ServerShuttingDownMessage : WebSocketMessage
{
    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.ServerShuttingDown;
}
