using System.ComponentModel;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Sync play command.
/// </summary>
public class SyncPlayCommandMessage : OutboundWebSocketMessage<SendCommand>
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
    [DefaultValue(SessionMessageType.SyncPlayCommand)]
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayCommand;
}
