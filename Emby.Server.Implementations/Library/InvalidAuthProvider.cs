using System.Threading.Tasks;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Net;

namespace Emby.Server.Implementations.Library
{
    public class InvalidAuthProvider : IAuthenticationProvider
    {
        public string Name => "InvalidOrMissingAuthenticationProvider";

        public bool IsEnabled => true;

        public Task<ProviderAuthenticationResult> Authenticate(string username, string password)
        {
            throw new AuthenticationException("User Account cannot login with this provider. The Normal provider for this user cannot be found");
        }

        public bool HasPassword(User user)
        {
            return true;
        }

        public Task ChangePassword(User user, string newPassword)
        {
            return Task.CompletedTask;
        }

        public void ChangeEasyPassword(User user, string newPassword, string newPasswordHash)
        {
            // Nothing here
        }

        public string GetPasswordHash(User user)
        {
            return string.Empty;
        }

        public string GetEasyPasswordHash(User user)
        {
            return string.Empty;
        }
    }
}
