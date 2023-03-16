using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Server restarting down message.
/// </summary>
public class ServerRestartingMessage : WebSocketMessage
{
    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.ServerRestarting;
}
