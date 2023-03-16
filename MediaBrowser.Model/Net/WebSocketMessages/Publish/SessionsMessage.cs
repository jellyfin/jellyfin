using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Sessions message.
/// </summary>
public class SessionsMessage : WebSocketMessage<SessionInfoModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionsMessage"/> class.
    /// </summary>
    /// <param name="data">Session info.</param>
    public SessionsMessage(SessionInfoModel data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.Sessions;
}
