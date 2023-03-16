using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Subscribe;

/// <summary>
/// Sessions start message.
/// </summary>
public class SessionsStartMessage : WebSocketMessage<SessionInfoModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionsStartMessage"/> class.
    /// </summary>
    /// <param name="data">Session info.</param>
    public SessionsStartMessage(SessionInfoModel data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.SessionsStart;
}
