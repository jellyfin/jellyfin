using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Entities;

namespace Emby.Server.Implementations.Library
{
    public class InvalidAuthProvider : IAuthenticationProvider
    {
        public string Name => "InvalidorMissingAuthenticationProvider";

        public bool IsEnabled => true;

        public Task<ProviderAuthenticationResult> Authenticate(string username, string password)
        {
            throw new Exception("User Account cannot login with this provider. The Normal provider for this user cannot be found");
        }

        public Task<bool> HasPassword(User user)
        {
            return Task.FromResult(true);
        }

        public Task ChangePassword(User user, string newPassword)
        {
            return Task.FromResult(true);
        }

        public void ChangeEasyPassword(User user, string newPassword, string newPasswordHash)
        {
            // Nothing here   
        }

        public string GetPasswordHash(User user)
        {
            return "";
        }

        public string GetEasyPasswordHash(User user)
        {
            return "";
        }
    }
}
