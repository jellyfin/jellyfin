using System.Threading.Tasks;
using Jellyfin.Data.Events.Users;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Server.Implementations.Events.Consumers.Users
{
    /// <summary>
    /// Provisions the default list when a user is created.
    /// </summary>
    public class UserListProvisioner : IEventConsumer<UserCreatedEventArgs>
    {
        private readonly IUserListManager _userListManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserListProvisioner"/> class.
        /// </summary>
        /// <param name="userListManager">The user list manager.</param>
        public UserListProvisioner(IUserListManager userListManager)
        {
            _userListManager = userListManager;
        }

        /// <inheritdoc />
        public async Task OnEvent(UserCreatedEventArgs eventArgs)
        {
            await _userListManager.GetOrCreateDefaultListAsync(eventArgs.Argument.Id).ConfigureAwait(false);
        }
    }
}
