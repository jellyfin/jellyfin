using System.Threading.Tasks;
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
        public bool HasPassword(Data.Entities.User user)
        {
            return true;
        }

        /// <inheritdoc />
        public Task ChangePassword(Data.Entities.User user, string newPassword)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void ChangeEasyPassword(Data.Entities.User user, string newPassword, string newPasswordHash)
        {
            // Nothing here
        }

        /// <inheritdoc />
        public string GetEasyPasswordHash(Data.Entities.User user)
        {
            return string.Empty;
        }
    }
}
