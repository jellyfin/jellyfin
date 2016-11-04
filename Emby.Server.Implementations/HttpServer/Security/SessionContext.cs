using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using System.Threading.Tasks;
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

        public Task<SessionInfo> GetSession(IServiceRequest requestContext)
        {
            var authorization = _authContext.GetAuthorizationInfo(requestContext);

            //if (!string.IsNullOrWhiteSpace(authorization.Token))
            //{
            //    var auth = GetTokenInfo(requestContext);
            //    if (auth != null)
            //    {
            //        return _sessionManager.GetSessionByAuthenticationToken(auth, authorization.DeviceId, requestContext.RemoteIp, authorization.Version);
            //    }
            //}

            var user = string.IsNullOrWhiteSpace(authorization.UserId) ? null : _userManager.GetUserById(authorization.UserId);
            return _sessionManager.LogSessionActivity(authorization.Client, authorization.Version, authorization.DeviceId, authorization.Device, requestContext.RemoteIp, user);
        }

        private AuthenticationInfo GetTokenInfo(IServiceRequest request)
        {
            object info;
            request.Items.TryGetValue("OriginalAuthenticationInfo", out info);
            return info as AuthenticationInfo;
        }

        public Task<SessionInfo> GetSession(object requestContext)
        {
            var req = new ServiceRequest((IRequest)requestContext);
            return GetSession(req);
        }

        public async Task<User> GetUser(IServiceRequest requestContext)
        {
            var session = await GetSession(requestContext).ConfigureAwait(false);

            return session == null || !session.UserId.HasValue ? null : _userManager.GetUserById(session.UserId.Value);
        }

        public Task<User> GetUser(object requestContext)
        {
            var req = new ServiceRequest((IRequest)requestContext);
            return GetUser(req);
        }
    }
}
