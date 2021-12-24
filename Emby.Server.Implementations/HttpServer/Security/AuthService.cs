#pragma warning disable CS1591

using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Http;

namespace Emby.Server.Implementations.HttpServer.Security
{
    public class AuthService : IAuthService
    {
        private readonly IAuthorizationContext _authorizationContext;

        public AuthService(
            IAuthorizationContext authorizationContext)
        {
            _authorizationContext = authorizationContext;
        }

        public async Task<AuthorizationInfo> Authenticate(HttpRequest request)
        {
            var auth = await _authorizationContext.GetAuthorizationInfo(request).ConfigureAwait(false);

            if (!auth.HasToken)
            {
                return auth;
            }

            if (!auth.IsAuthenticated)
            {
                throw new SecurityException("Invalid token.");
            }

            if (auth.User?.HasPermission(PermissionKind.IsDisabled) ?? false)
            {
                throw new SecurityException("User account has been disabled.");
            }

            return auth;
        }
    }
}
