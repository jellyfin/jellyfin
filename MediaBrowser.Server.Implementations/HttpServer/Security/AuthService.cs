using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Server.Implementations.HttpServer.Security
{
    public class AuthService : IAuthService
    {
        private readonly IServerConfigurationManager _config;

        public AuthService(IUserManager userManager, IAuthorizationContext authorizationContext, IServerConfigurationManager config, IConnectManager connectManager, ISessionManager sessionManager)
        {
            AuthorizationContext = authorizationContext;
            _config = config;
            SessionManager = sessionManager;
            ConnectManager = connectManager;
            UserManager = userManager;
        }

        public IUserManager UserManager { get; private set; }
        public IAuthorizationContext AuthorizationContext { get; private set; }
        public IConnectManager ConnectManager { get; private set; }
        public ISessionManager SessionManager { get; private set; }

        /// <summary>
        /// Redirect the client to a specific URL if authentication failed.
        /// If this property is null, simply `401 Unauthorized` is returned.
        /// </summary>
        public string HtmlRedirect { get; set; }

        public void Authenticate(IServiceRequest request,
            IAuthenticationAttributes authAttribtues)
        {
            ValidateUser(request, authAttribtues);
        }

        private void ValidateUser(IServiceRequest request,
            IAuthenticationAttributes authAttribtues)
        {
            // This code is executed before the service
            var auth = AuthorizationContext.GetAuthorizationInfo(request);

            if (!IsExemptFromAuthenticationToken(auth, authAttribtues))
            {
                var valid = IsValidConnectKey(auth.Token);

                if (!valid)
                {
                    ValidateSecurityToken(request, auth.Token);
                }
            }

            var user = string.IsNullOrWhiteSpace(auth.UserId)
                ? null
                : UserManager.GetUserById(auth.UserId);

            if (user == null & !string.IsNullOrWhiteSpace(auth.UserId))
            {
                throw new SecurityException("User with Id " + auth.UserId + " not found");
            }

            if (user != null)
            {
                if (user.Policy.IsDisabled)
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
                    request.AddResponseHeader("X-Application-Error-Code", "ParentalControl");
                    throw new SecurityException("This user account is not allowed access at this time.")
                    {
                        SecurityExceptionType = SecurityExceptionType.ParentalControl
                    };
                }
            }

            if (!IsExemptFromRoles(auth, authAttribtues))
            {
                var roles = authAttribtues.GetRoles().ToList();

                ValidateRoles(roles, user);
            }

            if (!string.IsNullOrWhiteSpace(auth.DeviceId) &&
                !string.IsNullOrWhiteSpace(auth.Client) &&
                !string.IsNullOrWhiteSpace(auth.Device))
            {
                SessionManager.LogSessionActivity(auth.Client,
                    auth.Version,
                    auth.DeviceId,
                    auth.Device,
                    request.RemoteIp,
                    user);
            }
        }

        private bool IsExemptFromAuthenticationToken(AuthorizationInfo auth, IAuthenticationAttributes authAttribtues)
        {
            if (!_config.Configuration.IsStartupWizardCompleted &&
                authAttribtues.AllowBeforeStartupWizard)
            {
                return true;
            }

            return _config.Configuration.InsecureApps7.Contains(auth.Client ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);
        }

        private bool IsExemptFromRoles(AuthorizationInfo auth, IAuthenticationAttributes authAttribtues)
        {
            if (!_config.Configuration.IsStartupWizardCompleted &&
                authAttribtues.AllowBeforeStartupWizard)
            {
                return true;
            }

            return false;
        }

        private void ValidateRoles(List<string> roles, User user)
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
        }

        private bool IsValidConnectKey(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            return ConnectManager.IsAuthorizationTokenValid(token);
        }

        private void ValidateSecurityToken(IServiceRequest request, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new SecurityException("Access token is invalid or expired.");
            }

            var info = (AuthenticationInfo)request.Items["OriginalAuthenticationInfo"];

            if (info == null)
            {
                throw new SecurityException("Access token is invalid or expired.");
            }

            if (!info.IsActive)
            {
                throw new SecurityException("Access token has expired.");
            }

            //if (!string.IsNullOrWhiteSpace(info.UserId))
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
