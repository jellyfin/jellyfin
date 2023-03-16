using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// User data changed message.
/// </summary>
public class UserDataChangedMessage : WebSocketMessage<UserDataChangeInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserDataChangedMessage"/> class.
    /// </summary>
    /// <param name="data">The data change info.</param>
    public UserDataChangedMessage(UserDataChangeInfo data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.UserDataChanged;
}
