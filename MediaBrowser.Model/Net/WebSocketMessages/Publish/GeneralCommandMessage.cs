using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// General command websocket message.
/// </summary>
public class GeneralCommandMessage : WebSocketMessage<GeneralCommand>
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
    public override SessionMessageType MessageType => SessionMessageType.GeneralCommand;
}
