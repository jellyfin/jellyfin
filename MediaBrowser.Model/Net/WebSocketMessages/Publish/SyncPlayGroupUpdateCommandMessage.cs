using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Untyped sync play command.
/// </summary>
public class SyncPlayGroupUpdateCommandMessage : WebSocketMessage<GroupUpdate>
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
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}
