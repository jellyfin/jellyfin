using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Sync play group update command with group info.
/// GroupUpdateTypes: GroupJoined.
/// </summary>
public class SyncPlayGroupUpdateCommandOfGroupInfoMessage : WebSocketMessage<GroupUpdate<GroupInfoDto>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayGroupUpdateCommandOfGroupInfoMessage"/> class.
    /// </summary>
    /// <param name="data">The group info.</param>
    public SyncPlayGroupUpdateCommandOfGroupInfoMessage(GroupUpdate<GroupInfoDto> data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}
