#nullable enable

using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Authentication;

namespace Jellyfin.Server.Implementations.Users
{
    /// <summary>
    /// An invalid authentication provider.
    /// </summary>
    public class InvalidAuthProvider : IAuthenticationProvider
    {
        /// <inheritdoc />
        public string Name => "InvalidOrMissingAuthenticationProvider";

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public Task<ProviderAuthenticationResult> Authenticate(string username, string password)
        {
            throw new AuthenticationException("User Account cannot login with this provider. The Normal provider for this user cannot be found");
        }

        /// <inheritdoc />
        public bool HasPassword(User user)
        {
            return true;
        }

        /// <inheritdoc />
        public Task ChangePassword(User user, string newPassword)
        {
            return Task.CompletedTask;
        }
    }
}
