using System;
using System.Linq;
using Emby.Server.Implementations.SocketSharp;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.HttpServer.Security
{
    /// <inheritdoc />
    public class AuthService : IAuthService
    {
        private readonly ILogger<AuthService> _logger;
        private readonly IAuthorizationContext _authorizationContext;
        private readonly ISessionManager _sessionManager;
        private readonly IServerConfigurationManager _config;
        private readonly INetworkManager _networkManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="authorizationContext">The authorization context.</param>
        /// <param name="config">The server configuration manager.</param>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="networkManager">The network manager.</param>
        public AuthService(
            ILogger<AuthService> logger,
            IAuthorizationContext authorizationContext,
            IServerConfigurationManager config,
            ISessionManager sessionManager,
            INetworkManager networkManager)
        {
            _logger = logger;
            _authorizationContext = authorizationContext;
            _config = config;
            _sessionManager = sessionManager;
            _networkManager = networkManager;
        }

        /// <inheritdoc />
        public void Authenticate(IRequest request, IAuthenticationAttributes authAttribtues)
        {
            ValidateUser(request, authAttribtues);
        }

        /// <inheritdoc />
        public User Authenticate(HttpRequest request, IAuthenticationAttributes authAttributes)
        {
            var req = new WebSocketSharpRequest(request, null, request.Path, _logger);
            var user = ValidateUser(req, authAttributes);
            return user;
        }

        private User ValidateUser(IRequest request, IAuthenticationAttributes authAttribtues)
        {
            // This code is executed before the service
            var auth = _authorizationContext.GetAuthorizationInfo(request);

            if (!IsExemptFromAuthenticationToken(authAttribtues, request))
            {
                ValidateSecurityToken(request, auth.Token);
            }

            if (authAttribtues.AllowLocalOnly && !request.IsLocal)
            {
                throw new SecurityException("Operation not found.");
            }

            var user = auth.User;

            if (user == null && auth.UserId != Guid.Empty)
            {
                throw new SecurityException("User with Id " + auth.UserId + " not found");
            }

            if (user != null)
            {
                ValidateUserAccess(user, request, authAttribtues);
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
                _sessionManager.LogSessionActivity(auth.Client,
                    auth.Version,
                    auth.DeviceId,
                    auth.Device,
                    request.RemoteIp,
                    user);
            }

            return user;
        }

        private void ValidateUserAccess(
            User user,
            IRequest request,
            IAuthenticationAttributes authAttributes)
        {
            if (user.Policy.IsDisabled)
            {
                throw new SecurityException("User account has been disabled.")
                {
                    SecurityExceptionType = SecurityExceptionType.Unauthenticated
                };
            }

            if (!user.Policy.EnableRemoteAccess && !_networkManager.IsInLocalNetwork(request.RemoteIp))
            {
                throw new SecurityException("User account has been disabled.")
                {
                    SecurityExceptionType = SecurityExceptionType.Unauthenticated
                };
            }

            if (!user.Policy.IsAdministrator
                && !authAttributes.EscapeParentalControl
                && !user.IsParentalScheduleAllowed())
            {
                request.Response.Headers.Add("X-Application-Error-Code", "ParentalControl");

                throw new SecurityException("This user account is not allowed access at this time.")
                {
                    SecurityExceptionType = SecurityExceptionType.ParentalControl
                };
            }
        }

        private bool IsExemptFromAuthenticationToken(IAuthenticationAttributes authAttributes, IRequest request)
        {
            if (!_config.Configuration.IsStartupWizardCompleted && authAttributes.AllowBeforeStartupWizard)
            {
                return true;
            }

            return (authAttributes.AllowLocal || authAttributes.AllowLocalOnly) && request.IsLocal;
        }

        private bool IsExemptFromRoles(AuthorizationInfo auth, IAuthenticationAttributes authAttributes, IRequest request, AuthenticationInfo tokenInfo)
        {
            if (!_config.Configuration.IsStartupWizardCompleted && authAttributes.AllowBeforeStartupWizard)
            {
                return true;
            }

            return ((authAttributes.AllowLocal || authAttributes.AllowLocalOnly) && request.IsLocal)
                   || string.IsNullOrEmpty(auth.Token)
                   || (tokenInfo != null && tokenInfo.UserId.Equals(Guid.Empty));
        }

        private static void ValidateRoles(string[] roles, User user)
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

        private static AuthenticationInfo GetTokenInfo(IRequest request)
        {
            request.Items.TryGetValue("OriginalAuthenticationInfo", out var info);
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
