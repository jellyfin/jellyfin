using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Sync play command.
/// TODO figure out all of the group update types...
/// </summary>
public class SyncPlayGroupUpdateCommandMessage : WebSocketMessage<object>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayGroupUpdateCommandMessage"/> class.
    /// </summary>
    /// <param name="data">The send command.</param>
    public SyncPlayGroupUpdateCommandMessage(object data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.SyncPlayGroupUpdate;
}
