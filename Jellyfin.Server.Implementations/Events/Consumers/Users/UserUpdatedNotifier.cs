using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Events.Users;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Rebus.Handlers;

namespace Jellyfin.Server.Implementations.Events.Consumers.Users
{
    /// <summary>
    /// Notifies a user when their account has been updated.
    /// </summary>
    public class UserUpdatedNotifier : IHandleMessages<UserUpdatedEventArgs>
    {
        private readonly IUserManager _userManager;
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserUpdatedNotifier"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="sessionManager">The session manager.</param>
        public UserUpdatedNotifier(IUserManager userManager, ISessionManager sessionManager)
        {
            _userManager = userManager;
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        public async Task Handle(UserUpdatedEventArgs message)
        {
            await _sessionManager.SendMessageToUserSessions(
                new List<Guid> { message.Argument.Id },
                SessionMessageType.UserUpdated,
                _userManager.GetUserDto(message.Argument),
                CancellationToken.None).ConfigureAwait(false);
        }
    }
}
