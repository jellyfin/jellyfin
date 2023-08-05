using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Playstate message.
/// </summary>
public class PlaystateMessage : OutboundWebSocketMessage<PlaystateRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaystateMessage"/> class.
    /// </summary>
    /// <param name="data">Playstate request data.</param>
    public PlaystateMessage(PlaystateRequest data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.Playstate)]
    public override SessionMessageType MessageType => SessionMessageType.Playstate;
}
