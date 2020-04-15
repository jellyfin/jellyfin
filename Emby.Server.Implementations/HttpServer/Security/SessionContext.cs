using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Services;

namespace Emby.Server.Implementations.HttpServer.Security
{
    /// <inheritdoc />
    public class SessionContext : ISessionContext
    {
        private readonly IUserManager _userManager;
        private readonly ISessionManager _sessionManager;
        private readonly IAuthorizationContext _authContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionContext"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="authContext">The authorization context.</param>
        /// <param name="sessionManager">The session manager.</param>
        public SessionContext(IUserManager userManager, IAuthorizationContext authContext, ISessionManager sessionManager)
        {
            _userManager = userManager;
            _authContext = authContext;
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        public SessionInfo GetSession(IRequest requestContext)
        {
            var authorization = _authContext.GetAuthorizationInfo(requestContext);

            var user = authorization.User;
            return _sessionManager.LogSessionActivity(authorization.Client, authorization.Version, authorization.DeviceId, authorization.Device, requestContext.RemoteIp, user);
        }

        /// <inheritdoc />
        public SessionInfo GetSession(object requestContext)
        {
            return GetSession((IRequest)requestContext);
        }

        /// <inheritdoc />
        public User GetUser(IRequest requestContext)
        {
            var session = GetSession(requestContext);

            return session == null || session.UserId.Equals(Guid.Empty) ? null : _userManager.GetUserById(session.UserId);
        }

        /// <inheritdoc />
        public User GetUser(object requestContext)
        {
            return GetUser((IRequest)requestContext);
        }
    }
}
