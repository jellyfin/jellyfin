using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Subscribe;

/// <summary>
/// Sessions start message.
/// TODO use SessionInfo for Data.
/// </summary>
public class SessionsStartMessage : WebSocketMessage<object>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionsStartMessage"/> class.
    /// </summary>
    /// <param name="data">Session info.</param>
    public SessionsStartMessage(object data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.SessionsStart;
}
