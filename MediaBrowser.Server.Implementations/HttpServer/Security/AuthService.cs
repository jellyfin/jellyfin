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

        public void Authenticate(IRequest request,
            IResponse response,
            object requestDto,
            IAuthenticated authAttribtues)
        {
            if (HostContext.HasValidAuthSecret(request))
                return;

            //ExecuteBasic(req, res, requestDto); //first check if session is authenticated
            //if (res.IsClosed) return; //AuthenticateAttribute already closed the request (ie auth failed)

            ValidateUser(request, response, authAttribtues);
        }

        private void ValidateUser(IRequest req, IResponse response, IAuthenticated authAttribtues)
        {
            // This code is executed before the service
            var auth = AuthorizationContext.GetAuthorizationInfo(req);

            if (!authAttribtues.AllowLocal || !req.IsLocal)
            {
                if (!string.IsNullOrWhiteSpace(auth.Token) ||
                    !_config.Configuration.InsecureApps3.Contains(auth.Client ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                {
                    if (!IsValidConnectKey(auth.Token))
                    {
                        SessionManager.ValidateSecurityToken(auth.Token);
                    }
                }
            }

            var user = string.IsNullOrWhiteSpace(auth.UserId)
                ? null
                : UserManager.GetUserById(auth.UserId);

            if (user == null & !string.IsNullOrWhiteSpace(auth.UserId))
            {
                throw new ArgumentException("User with Id " + auth.UserId + " not found");
            }

            if (user != null)
            {
                if (user.Configuration.IsDisabled)
                {
                    throw new AuthenticationException("User account has been disabled.");
                }

                if (!user.Configuration.IsAdministrator &&
                    !authAttribtues.EscapeParentalControl &&
                    !user.IsParentalScheduleAllowed())
                {
                    response.AddHeader("X-Application-Error-Code", "ParentalControl");
                    throw new AuthenticationException("This user account is not allowed access at this time.");
                }
            }

            var roles = authAttribtues.GetRoles().ToList();

            if (roles.Contains("admin", StringComparer.OrdinalIgnoreCase))
            {
                if (user == null || !user.Configuration.IsAdministrator)
                {
                    throw new ArgumentException("Administrative access is required for this request.");
                }
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

        private bool IsValidConnectKey(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                return UserManager.Users.Any(u => string.Equals(token, u.ConnectAccessKey, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(u.ConnectAccessKey));
            }

            return false;
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
