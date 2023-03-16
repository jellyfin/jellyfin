using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Sync play group update command with group state update.
/// GroupUpdateTypes: StateUpdate.
/// </summary>
public class SyncPlayGroupUpdateCommandOfGroupStateUpdateMessage : WebSocketMessage<GroupUpdate<GroupStateUpdate>>
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
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}
