using System.ComponentModel;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Sync play group update command with group state update.
/// GroupUpdateTypes: StateUpdate.
/// </summary>
public class SyncPlayGroupUpdateCommandOfGroupStateUpdateMessage : OutboundWebSocketMessage<GroupUpdate<GroupStateUpdate>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayGroupUpdateCommandOfGroupStateUpdateMessage"/> class.
    /// </summary>
    /// <param name="data">The group info.</param>
    public SyncPlayGroupUpdateCommandOfGroupStateUpdateMessage(GroupUpdate<GroupStateUpdate> data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.SyncPlayGroupUpdate)]
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}
