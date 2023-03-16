using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Playstate message.
/// </summary>
public class PlaystateMessage : WebSocketMessage<PlaystateRequest>
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
    public override SessionMessageType MessageType => SessionMessageType.Playstate;
}
