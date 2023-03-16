using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Subscribe;

/// <summary>
/// Sessions stop message.
/// </summary>
public class SessionsStopMessage : WebSocketMessage<SessionInfoModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionsStopMessage"/> class.
    /// </summary>
    /// <param name="data">Session info.</param>
    public SessionsStopMessage(SessionInfoModel data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.SessionsStop;
}
