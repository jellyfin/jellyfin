using System;
using System.Threading.Tasks;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Entities;

namespace Emby.Server.Implementations.Library
{
    public class DefaultPasswordResetProvider : IPasswordResetProvider
    {
        public string Name => "Default Password Reset";

        public bool IsEnabled => true;

        // set our default timeout to an hour since we'll be making the PIN it generates a little less fragile
        public TimeSpan PasswordResetTimeout => new TimeSpan(1,0,0);

        public Task ResetPassword(User user)
        {
            throw new NotImplementedException();
        }
    }
}
