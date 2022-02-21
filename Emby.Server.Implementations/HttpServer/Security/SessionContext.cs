#pragma warning disable CS1591

using System;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using Microsoft.AspNetCore.Http;

namespace Emby.Server.Implementations.HttpServer.Security
{
    public class SessionContext : ISessionContext
    {
        private readonly IUserManager _userManager;
        private readonly ISessionManager _sessionManager;
        private readonly IAuthorizationContext _authContext;

        public SessionContext(IUserManager userManager, IAuthorizationContext authContext, ISessionManager sessionManager)
        {
            _userManager = userManager;
            _authContext = authContext;
            _sessionManager = sessionManager;
        }

        public async Task<SessionInfo> GetSession(HttpContext requestContext)
        {
            var authorization = await _authContext.GetAuthorizationInfo(requestContext).ConfigureAwait(false);

            var user = authorization.User;
            return await _sessionManager.LogSessionActivity(
                authorization.Client,
                authorization.Version,
                authorization.DeviceId,
                authorization.Device,
                requestContext.GetNormalizedRemoteIp().ToString(),
                user).ConfigureAwait(false);
        }

        public Task<SessionInfo> GetSession(object requestContext)
        {
            return GetSession((HttpContext)requestContext);
        }

        public async Task<User?> GetUser(HttpContext requestContext)
        {
            var session = await GetSession(requestContext).ConfigureAwait(false);

            return session.UserId.Equals(default)
                ? null
                : _userManager.GetUserById(session.UserId);
        }

        public Task<User?> GetUser(object requestContext)
        {
            return GetUser(((HttpRequest)requestContext).HttpContext);
        }
    }
}
