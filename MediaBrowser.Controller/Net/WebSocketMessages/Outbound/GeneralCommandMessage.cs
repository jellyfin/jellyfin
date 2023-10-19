using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// General command websocket message.
/// </summary>
public class GeneralCommandMessage : OutboundWebSocketMessage<GeneralCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeneralCommandMessage"/> class.
    /// </summary>
    /// <param name="data">The general command.</param>
    public GeneralCommandMessage(GeneralCommand data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.GeneralCommand)]
    public override SessionMessageType MessageType => SessionMessageType.GeneralCommand;
}
