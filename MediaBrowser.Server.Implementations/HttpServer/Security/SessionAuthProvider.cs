using MediaBrowser.Controller.Net;
using ServiceStack;
using ServiceStack.Auth;

namespace MediaBrowser.Server.Implementations.HttpServer.Security
{
    public class SessionAuthProvider : CredentialsAuthProvider
    {
        private readonly ISessionContext _sessionContext;

        public SessionAuthProvider(ISessionContext sessionContext)
        {
            _sessionContext = sessionContext;
        }

        public override bool TryAuthenticate(IServiceBase authService, string userName, string password)
        {
            return true;
        }

        public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null)
        {
            return true;
        }

        protected override void SaveUserAuth(IServiceBase authService, IAuthSession session, IAuthRepository authRepo, IAuthTokens tokens)
        {
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            return base.Authenticate(authService, session, request);
        }
    }
}
