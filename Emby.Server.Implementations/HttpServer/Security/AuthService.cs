using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using System;
using System.Linq;
using MediaBrowser.Model.Services;
using MediaBrowser.Common.Net;

namespace Emby.Server.Implementations.HttpServer.Security
{
    public class AuthService : IAuthService
    {
        private readonly IServerConfigurationManager _config;

        public AuthService(IUserManager userManager, IAuthorizationContext authorizationContext, IServerConfigurationManager config, ISessionManager sessionManager, INetworkManager networkManager)
        {
            AuthorizationContext = authorizationContext;
            _config = config;
            SessionManager = sessionManager;
            UserManager = userManager;
            NetworkManager = networkManager;
        }

        public IUserManager UserManager { get; private set; }
        public IAuthorizationContext AuthorizationContext { get; private set; }
        public ISessionManager SessionManager { get; private set; }
        public INetworkManager NetworkManager { get; private set; }

        /// <summary>
        /// Redirect the client to a specific URL if authentication failed.
        /// If this property is null, simply `401 Unauthorized` is returned.
        /// </summary>
        public string HtmlRedirect { get; set; }

        public void Authenticate(IRequest request, IAuthenticationAttributes authAttribtues)
        {
            ValidateUser(request, authAttribtues);
        }

        private void ValidateUser(IRequest request, IAuthenticationAttributes authAttribtues)
        {
            // This code is executed before the service
            var auth = AuthorizationContext.GetAuthorizationInfo(request);

            if (!IsExemptFromAuthenticationToken(auth, authAttribtues, request))
            {
                ValidateSecurityToken(request, auth.Token);
            }

            if (authAttribtues.AllowLocalOnly && !request.IsLocal)
            {
                throw new SecurityException("Operation not found.");
            }

            var user = auth.User;

            if (user == null & !auth.UserId.Equals(Guid.Empty))
            {
                throw new SecurityException("User with Id " + auth.UserId + " not found");
            }

            if (user != null)
            {
                ValidateUserAccess(user, request, authAttribtues, auth);
            }

            var info = GetTokenInfo(request);

            if (!IsExemptFromRoles(auth, authAttribtues, request, info))
            {
                var roles = authAttribtues.GetRoles();

                ValidateRoles(roles, user);
            }

            if (!string.IsNullOrEmpty(auth.DeviceId) &&
                !string.IsNullOrEmpty(auth.Client) &&
                !string.IsNullOrEmpty(auth.Device))
            {
                SessionManager.LogSessionActivity(auth.Client,
                    auth.Version,
                    auth.DeviceId,
                    auth.Device,
                    request.RemoteIp,
                    user);
            }
        }

        private void ValidateUserAccess(User user, IRequest request,
            IAuthenticationAttributes authAttribtues,
            AuthorizationInfo auth)
        {
            if (user.Policy.IsDisabled)
            {
                throw new SecurityException("User account has been disabled.")
                {
                    SecurityExceptionType = SecurityExceptionType.Unauthenticated
                };
            }

            if (!user.Policy.EnableRemoteAccess && !NetworkManager.IsInLocalNetwork(request.RemoteIp))
            {
                throw new SecurityException("User account has been disabled.")
                {
                    SecurityExceptionType = SecurityExceptionType.Unauthenticated
                };
            }

            if (!user.Policy.IsAdministrator &&
                !authAttribtues.EscapeParentalControl &&
                !user.IsParentalScheduleAllowed())
            {
                request.Response.AddHeader("X-Application-Error-Code", "ParentalControl");

                throw new SecurityException("This user account is not allowed access at this time.")
                {
                    SecurityExceptionType = SecurityExceptionType.ParentalControl
                };
            }
        }

        private bool IsExemptFromAuthenticationToken(AuthorizationInfo auth, IAuthenticationAttributes authAttribtues, IRequest request)
        {
            if (!_config.Configuration.IsStartupWizardCompleted && authAttribtues.AllowBeforeStartupWizard)
            {
                return true;
            }

            if (authAttribtues.AllowLocal && request.IsLocal)
            {
                return true;
            }
            if (authAttribtues.AllowLocalOnly && request.IsLocal)
            {
                return true;
            }

            return false;
        }

        private bool IsExemptFromRoles(AuthorizationInfo auth, IAuthenticationAttributes authAttribtues, IRequest request, AuthenticationInfo tokenInfo)
        {
            if (!_config.Configuration.IsStartupWizardCompleted && authAttribtues.AllowBeforeStartupWizard)
            {
                return true;
            }

            if (authAttribtues.AllowLocal && request.IsLocal)
            {
                return true;
            }

            if (authAttribtues.AllowLocalOnly && request.IsLocal)
            {
                return true;
            }

            if (string.IsNullOrEmpty(auth.Token))
            {
                return true;
            }

            if (tokenInfo != null && tokenInfo.UserId.Equals(Guid.Empty))
            {
                return true;
            }

            return false;
        }

        private void ValidateRoles(string[] roles, User user)
        {
            if (roles.Contains("admin", StringComparer.OrdinalIgnoreCase))
            {
                if (user == null || !user.Policy.IsAdministrator)
                {
                    throw new SecurityException("User does not have admin access.")
                    {
                        SecurityExceptionType = SecurityExceptionType.Unauthenticated
                    };
                }
            }
            if (roles.Contains("delete", StringComparer.OrdinalIgnoreCase))
            {
                if (user == null || !user.Policy.EnableContentDeletion)
                {
                    throw new SecurityException("User does not have delete access.")
                    {
                        SecurityExceptionType = SecurityExceptionType.Unauthenticated
                    };
                }
            }
            if (roles.Contains("download", StringComparer.OrdinalIgnoreCase))
            {
                if (user == null || !user.Policy.EnableContentDownloading)
                {
                    throw new SecurityException("User does not have download access.")
                    {
                        SecurityExceptionType = SecurityExceptionType.Unauthenticated
                    };
                }
            }
        }

        private AuthenticationInfo GetTokenInfo(IRequest request)
        {
            object info;
            request.Items.TryGetValue("OriginalAuthenticationInfo", out info);
            return info as AuthenticationInfo;
        }

        private void ValidateSecurityToken(IRequest request, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new SecurityException("Access token is required.");
            }

            var info = GetTokenInfo(request);

            if (info == null)
            {
                throw new SecurityException("Access token is invalid or expired.");
            }

            //if (!string.IsNullOrEmpty(info.UserId))
            //{
            //    var user = _userManager.GetUserById(info.UserId);

            //    if (user == null || user.Configuration.IsDisabled)
            //    {
            //        throw new SecurityException("User account has been disabled.");
            //    }
            //}
        }
    }
}
