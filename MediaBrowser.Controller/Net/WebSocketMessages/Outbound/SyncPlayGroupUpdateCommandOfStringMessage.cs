using System.ComponentModel;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Sync play group update command with string.
/// GroupUpdateTypes: GroupDoesNotExist (error), LibraryAccessDenied (error), NotInGroup (error), GroupLeft (groupId), UserJoined (username), UserLeft (username).
/// </summary>
public class SyncPlayGroupUpdateCommandOfStringMessage : OutboundWebSocketMessage<GroupUpdate<string>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayGroupUpdateCommandOfStringMessage"/> class.
    /// </summary>
    /// <param name="data">The send command.</param>
    public SyncPlayGroupUpdateCommandOfStringMessage(GroupUpdate<string> data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.SyncPlayGroupUpdate)]
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}
