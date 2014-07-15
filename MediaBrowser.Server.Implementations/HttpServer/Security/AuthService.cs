using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Web;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace MediaBrowser.Server.Implementations.HttpServer.Security
{
    public class AuthService : IAuthService
    {
        private readonly IServerConfigurationManager _config;

        public AuthService(IUserManager userManager, ISessionManager sessionManager, IAuthorizationContext authorizationContext, IServerConfigurationManager config)
        {
            AuthorizationContext = authorizationContext;
            _config = config;
            SessionManager = sessionManager;
            UserManager = userManager;
        }

        public IUserManager UserManager { get; private set; }
        public ISessionManager SessionManager { get; private set; }
        public IAuthorizationContext AuthorizationContext { get; private set; }

        /// <summary>
        /// Restrict authentication to a specific <see cref="IAuthProvider"/>.
        /// For example, if this attribute should only permit access
        /// if the user is authenticated with <see cref="BasicAuthProvider"/>,
        /// you should set this property to <see cref="BasicAuthProvider.Name"/>.
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Redirect the client to a specific URL if authentication failed.
        /// If this property is null, simply `401 Unauthorized` is returned.
        /// </summary>
        public string HtmlRedirect { get; set; }

        public void Authenticate(IRequest req, IResponse res, object requestDto)
        {
            if (HostContext.HasValidAuthSecret(req))
                return;

            //ExecuteBasic(req, res, requestDto); //first check if session is authenticated
            //if (res.IsClosed) return; //AuthenticateAttribute already closed the request (ie auth failed)

            ValidateUser(req);
        }

        private void ValidateUser(IRequest req)
        {
            //This code is executed before the service
            var auth = AuthorizationContext.GetAuthorizationInfo(req);

            if (!string.IsNullOrWhiteSpace(auth.Token) || _config.Configuration.EnableTokenAuthentication)
            {
                SessionManager.ValidateSecurityToken(auth.Token);
            }

            var user = string.IsNullOrWhiteSpace(auth.UserId)
                ? null
                : UserManager.GetUserById(new Guid(auth.UserId));

            if (user == null & !string.IsNullOrWhiteSpace(auth.UserId))
            {
                // TODO: Re-enable
                //throw new ArgumentException("User with Id " + auth.UserId + " not found");
            }

            if (user != null && user.Configuration.IsDisabled)
            {
                throw new UnauthorizedAccessException("User account has been disabled.");
            }

            if (!string.IsNullOrWhiteSpace(auth.DeviceId) &&
                !string.IsNullOrWhiteSpace(auth.Client) &&
                !string.IsNullOrWhiteSpace(auth.Device))
            {
                SessionManager.LogSessionActivity(auth.Client,
                    auth.Version,
                    auth.DeviceId,
                    auth.Device,
                    req.RemoteIp,
                    user);
            }
        }

        private void ExecuteBasic(IRequest req, IResponse res, object requestDto)
        {
            if (AuthenticateService.AuthProviders == null)
                throw new InvalidOperationException(
                    "The AuthService must be initialized by calling AuthService.Init to use an authenticate attribute");

            var matchingOAuthConfigs = AuthenticateService.AuthProviders.Where(x =>
                this.Provider.IsNullOrEmpty()
                || x.Provider == this.Provider).ToList();

            if (matchingOAuthConfigs.Count == 0)
            {
                res.WriteError(req, requestDto, "No OAuth Configs found matching {0} provider"
                    .Fmt(this.Provider ?? "any"));
                res.EndRequest();
            }

            matchingOAuthConfigs.OfType<IAuthWithRequest>()
                .Each(x => x.PreAuthenticate(req, res));

            var session = req.GetSession();
            if (session == null || !matchingOAuthConfigs.Any(x => session.IsAuthorized(x.Provider)))
            {
                if (this.DoHtmlRedirectIfConfigured(req, res, true)) return;

                AuthProvider.HandleFailedAuth(matchingOAuthConfigs[0], session, req, res);
            }
        }

        protected bool DoHtmlRedirectIfConfigured(IRequest req, IResponse res, bool includeRedirectParam = false)
        {
            var htmlRedirect = this.HtmlRedirect ?? AuthenticateService.HtmlRedirect;
            if (htmlRedirect != null && req.ResponseContentType.MatchesContentType(MimeTypes.Html))
            {
                DoHtmlRedirect(htmlRedirect, req, res, includeRedirectParam);
                return true;
            }
            return false;
        }

        public static void DoHtmlRedirect(string redirectUrl, IRequest req, IResponse res, bool includeRedirectParam)
        {
            var url = req.ResolveAbsoluteUrl(redirectUrl);
            if (includeRedirectParam)
            {
                var absoluteRequestPath = req.ResolveAbsoluteUrl("~" + req.PathInfo + ToQueryString(req.QueryString));
                url = url.AddQueryParam(HostContext.ResolveLocalizedString(LocalizedStrings.Redirect), absoluteRequestPath);
            }

            res.RedirectToUrl(url);
        }

        private static string ToQueryString(INameValueCollection queryStringCollection)
        {
            return ToQueryString((NameValueCollection)queryStringCollection.Original);
        }

        private static string ToQueryString(NameValueCollection queryStringCollection)
        {
            if (queryStringCollection == null || queryStringCollection.Count == 0)
                return String.Empty;

            return "?" + queryStringCollection.ToFormUrlEncoded();
        }
    }
}
