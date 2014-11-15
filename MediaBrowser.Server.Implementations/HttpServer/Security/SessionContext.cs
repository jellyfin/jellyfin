using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using ServiceStack.Web;

namespace MediaBrowser.Server.Implementations.HttpServer.Security
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

        public SessionInfo GetSession(IServiceRequest requestContext)
        {
            var authorization = _authContext.GetAuthorizationInfo(requestContext);

            return _sessionManager.GetSession(authorization.DeviceId, authorization.Client, authorization.Version);
        }

        public User GetUser(IServiceRequest requestContext)
        {
            var session = GetSession(requestContext);

            return session == null || !session.UserId.HasValue ? null : _userManager.GetUserById(session.UserId.Value);
        }

        public SessionInfo GetSession(object requestContext)
        {
            var req = new ServiceStackServiceRequest((IRequest)requestContext);
            return GetSession(req);
        }

        public User GetUser(object requestContext)
        {
            var req = new ServiceStackServiceRequest((IRequest)requestContext);
            return GetUser(req);
        }
    }
}
