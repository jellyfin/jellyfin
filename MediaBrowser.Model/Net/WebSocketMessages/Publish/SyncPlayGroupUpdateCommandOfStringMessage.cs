using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Sync play group update command with string.
/// GroupUpdateTypes: GroupDoesNotExist (error), LibraryAccessDenied (error), NotInGroup (error), GroupLeft (groupId), UserJoined (username), UserLeft (username).
/// </summary>
public class SyncPlayGroupUpdateCommandOfStringMessage : WebSocketMessage<GroupUpdate<string>>
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
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}
