using System.ComponentModel;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Sync play group update command with play queue update.
/// GroupUpdateTypes: PlayQueue.
/// </summary>
public class SyncPlayGroupUpdateCommandOfPlayQueueUpdateMessage : OutboundWebSocketMessage<GroupUpdate<PlayQueueUpdate>>
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
    [DefaultValue(SessionMessageType.SyncPlayGroupUpdate)]
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}
