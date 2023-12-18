using System.ComponentModel;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Untyped sync play command.
/// </summary>
public class SyncPlayGroupUpdateCommandMessage : OutboundWebSocketMessage<GroupUpdate>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayGroupUpdateCommandMessage"/> class.
    /// </summary>
    /// <param name="data">The send command.</param>
    public SyncPlayGroupUpdateCommandMessage(GroupUpdate data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.SyncPlayGroupUpdate)]
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}
