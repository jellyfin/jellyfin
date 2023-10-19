using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Inbound;

/// <summary>
/// Sessions start message.
/// Data is the timing data encoded as "$initialDelay,$interval" in ms.
/// </summary>
public class SessionsStartMessage : InboundWebSocketMessage<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionsStartMessage"/> class.
    /// </summary>
    /// <param name="data">The timing data encoded as $initialDelay,$interval.</param>
    public SessionsStartMessage(string data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.SessionsStart)]
    public override SessionMessageType MessageType => SessionMessageType.SessionsStart;
}
