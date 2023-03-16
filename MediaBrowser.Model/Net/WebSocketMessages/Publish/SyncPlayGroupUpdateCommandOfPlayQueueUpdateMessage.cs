using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Sync play group update command with play queue update.
/// GroupUpdateTypes: PlayQueue.
/// </summary>
public class SyncPlayGroupUpdateCommandOfPlayQueueUpdateMessage : WebSocketMessage<GroupUpdate<PlayQueueUpdate>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayGroupUpdateCommandOfPlayQueueUpdateMessage"/> class.
    /// </summary>
    /// <param name="data">The play queue update.</param>
    public SyncPlayGroupUpdateCommandOfPlayQueueUpdateMessage(GroupUpdate<PlayQueueUpdate> data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}
