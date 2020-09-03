#pragma warning disable CS1591

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

        public AuthorizationInfo Authenticate(HttpRequest request)
        {
            var auth = _authorizationContext.GetAuthorizationInfo(request);
            if (auth?.User == null)
            {
                return null;
            }

            if (auth.User.HasPermission(PermissionKind.IsDisabled))
            {
                throw new SecurityException("User account has been disabled.");
            }

            return auth;
        }
    }
}
