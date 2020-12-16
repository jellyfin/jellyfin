#pragma warning disable CS1591

using System;
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

        public SessionInfo GetSession(HttpContext requestContext)
        {
            var authorization = _authContext.GetAuthorizationInfo(requestContext);

            var user = authorization.User;
            return _sessionManager.LogSessionActivity(authorization.Client, authorization.Version, authorization.DeviceId, authorization.Device, requestContext.GetNormalizedRemoteIp(), user);
        }

        public SessionInfo GetSession(object requestContext)
        {
            return GetSession((HttpContext)requestContext);
        }

        public User GetUser(HttpContext requestContext)
        {
            var session = GetSession(requestContext);

            return session == null || session.UserId.Equals(Guid.Empty) ? null : _userManager.GetUserById(session.UserId);
        }

        public User GetUser(object requestContext)
        {
            return GetUser(((HttpRequest)requestContext).HttpContext);
        }
    }
}
