using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// User updated message.
/// </summary>
public class UserUpdatedMessage : WebSocketMessage<UserDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserUpdatedMessage"/> class.
    /// </summary>
    /// <param name="data">The user dto.</param>
    public UserUpdatedMessage(UserDto data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.UserUpdated;
}
