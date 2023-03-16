using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Play command websocket message.
/// </summary>
public class PlayMessage : WebSocketMessage<PlayRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayMessage"/> class.
    /// </summary>
    /// <param name="data">The play request.</param>
    public PlayMessage(PlayRequest data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.Play;
}
