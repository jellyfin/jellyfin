using System;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// User deleted message.
/// </summary>
public class UserDeletedMessage : WebSocketMessage<Guid>
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
    public override SessionMessageType MessageType => SessionMessageType.UserDeleted;
}
