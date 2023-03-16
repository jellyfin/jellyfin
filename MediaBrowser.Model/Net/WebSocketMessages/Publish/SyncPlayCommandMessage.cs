using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Sync play command.
/// </summary>
public class SyncPlayCommandMessage : WebSocketMessage<SendCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayCommandMessage"/> class.
    /// </summary>
    /// <param name="data">The send command.</param>
    public SyncPlayCommandMessage(SendCommand data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayCommand;
}
