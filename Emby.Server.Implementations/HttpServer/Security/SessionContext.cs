#pragma warning disable CS1591

using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Services;

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

        public SessionInfo GetSession(IRequest requestContext)
        {
            var authorization = _authContext.GetAuthorizationInfo(requestContext);

            var user = authorization.User;
            return _sessionManager.LogSessionActivity(authorization.Client, authorization.Version, authorization.DeviceId, authorization.Device, requestContext.RemoteIp, user);
        }

        private AuthenticationInfo GetTokenInfo(IRequest request)
        {
            request.Items.TryGetValue("OriginalAuthenticationInfo", out var info);
            return info as AuthenticationInfo;
        }

        public SessionInfo GetSession(object requestContext)
        {
            return GetSession((IRequest)requestContext);
        }

        public User GetUser(IRequest requestContext)
        {
            var session = GetSession(requestContext);

            return session == null || session.UserId.Equals(Guid.Empty) ? null : _userManager.GetUserById(session.UserId);
        }

        public User GetUser(object requestContext)
        {
            return GetUser((IRequest)requestContext);
        }
    }
}
