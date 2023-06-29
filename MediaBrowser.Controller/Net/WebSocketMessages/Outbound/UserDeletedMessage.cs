using System;
using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// User deleted message.
/// </summary>
public class UserDeletedMessage : OutboundWebSocketMessage<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserDeletedMessage"/> class.
    /// </summary>
    /// <param name="data">The user id.</param>
    public UserDeletedMessage(Guid data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.UserDeleted)]
    public override SessionMessageType MessageType => SessionMessageType.UserDeleted;
}
