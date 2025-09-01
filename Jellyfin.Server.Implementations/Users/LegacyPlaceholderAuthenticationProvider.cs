using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Authentication;

namespace Jellyfin.Server.Implementations.Users
{
    [Obsolete("Only used as stand-in for legacy authentication providers. Remove when those are removed.")]
    internal class LegacyPlaceholderAuthenticationProvider : IAuthenticationProvider<UsernamePasswordAuthData>
    {
        public string Name => throw new NotImplementedException();

        public string? AuthenticationType => null;

        public Task<AuthenticationResult> Authenticate(UsernamePasswordAuthData authenticationData)
        {
            throw new NotImplementedException();
        }
    }
}
