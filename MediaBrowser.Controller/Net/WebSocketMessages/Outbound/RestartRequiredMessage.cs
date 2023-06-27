using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Restart required.
/// </summary>
public class RestartRequiredMessage : OutboundWebSocketMessage
{
    /// <inheritdoc />
    [DefaultValue(SessionMessageType.RestartRequired)]
    public override SessionMessageType MessageType => SessionMessageType.RestartRequired;
}
