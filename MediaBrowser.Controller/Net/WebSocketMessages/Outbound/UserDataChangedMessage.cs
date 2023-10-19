using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// User data changed message.
/// </summary>
public class UserDataChangedMessage : OutboundWebSocketMessage<UserDataChangeInfo>
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
    [DefaultValue(SessionMessageType.UserDataChanged)]
    public override SessionMessageType MessageType => SessionMessageType.UserDataChanged;
}
