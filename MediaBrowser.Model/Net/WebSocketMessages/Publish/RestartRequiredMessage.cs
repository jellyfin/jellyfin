using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Restart required.
/// </summary>
public class RestartRequiredMessage : WebSocketMessage
{
    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.RestartRequired;
}
