#pragma warning disable CS1591

using System;
using System.Linq;
using Emby.Server.Implementations.SocketSharp;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;

namespace Emby.Server.Implementations.HttpServer.Security
{
    public class AuthService : IAuthService
    {
        private readonly IAuthorizationContext _authorizationContext;
        private readonly ISessionManager _sessionManager;
        private readonly IServerConfigurationManager _config;
        private readonly INetworkManager _networkManager;

        public AuthService(
            IAuthorizationContext authorizationContext,
            IServerConfigurationManager config,
            ISessionManager sessionManager,
            INetworkManager networkManager)
        {
            _authorizationContext = authorizationContext;
            _config = config;
            _sessionManager = sessionManager;
            _networkManager = networkManager;
        }

        public void Authenticate(IRequest request, IAuthenticationAttributes authAttributes)
        {
            ValidateUser(request, authAttributes);
        }

        public User Authenticate(HttpRequest request, IAuthenticationAttributes authAttributes)
        {
            var req = new WebSocketSharpRequest(request, null, request.Path);
            var user = ValidateUser(req, authAttributes);
            return user;
        }

        public AuthorizationInfo Authenticate(HttpRequest request)
        {
            var auth = _authorizationContext.GetAuthorizationInfo(request);
            if (auth?.User == null)
            {
                return null;
            }

            if (auth.User.HasPermission(PermissionKind.IsDisabled))
            {
                throw new SecurityException("User account has been disabled.");
            }

            return auth;
        }

        private User ValidateUser(IRequest request, IAuthenticationAttributes authAttributes)
        {
            // This code is executed before the service
            var auth = _authorizationContext.GetAuthorizationInfo(request);

            if (!IsExemptFromAuthenticationToken(authAttributes, request))
            {
                ValidateSecurityToken(request, auth.Token);
            }

            if (authAttributes.AllowLocalOnly && !request.IsLocal)
            {
                throw new SecurityException("Operation not found.");
            }

            var user = auth.User;

            if (user == null && auth.UserId != Guid.Empty)
            {
                throw new AuthenticationException("User with Id " + auth.UserId + " not found");
            }

            if (user != null)
            {
                ValidateUserAccess(user, request, authAttributes);
            }

            var info = GetTokenInfo(request);

            if (!IsExemptFromRoles(auth, authAttributes, request, info))
            {
                var roles = authAttributes.GetRoles();

                ValidateRoles(roles, user);
            }

            if (!string.IsNullOrEmpty(auth.DeviceId) &&
                !string.IsNullOrEmpty(auth.Client) &&
                !string.IsNullOrEmpty(auth.Device))
            {
                _sessionManager.LogSessionActivity(
                    auth.Client,
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
            if (user.HasPermission(PermissionKind.IsDisabled))
            {
                throw new SecurityException("User account has been disabled.");
            }

            if (!user.HasPermission(PermissionKind.EnableRemoteAccess) && !_networkManager.IsInLocalNetwork(request.RemoteIp))
            {
                throw new SecurityException("User account has been disabled.");
            }

            if (!user.HasPermission(PermissionKind.IsAdministrator)
                && !authAttributes.EscapeParentalControl
                && !user.IsParentalScheduleAllowed())
            {
                request.Response.Headers.Add("X-Application-Error-Code", "ParentalControl");

                throw new SecurityException("This user account is not allowed access at this time.");
            }
        }

        private bool IsExemptFromAuthenticationToken(IAuthenticationAttributes authAttribtues, IRequest request)
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

            if (authAttribtues.IgnoreLegacyAuth)
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

        private static void ValidateRoles(string[] roles, User user)
        {
            if (roles.Contains("admin", StringComparer.OrdinalIgnoreCase))
            {
                if (user == null || !user.HasPermission(PermissionKind.IsAdministrator))
                {
                    throw new SecurityException("User does not have admin access.");
                }
            }

            if (roles.Contains("delete", StringComparer.OrdinalIgnoreCase))
            {
                if (user == null || !user.HasPermission(PermissionKind.EnableContentDeletion))
                {
                    throw new SecurityException("User does not have delete access.");
                }
            }

            if (roles.Contains("download", StringComparer.OrdinalIgnoreCase))
            {
                if (user == null || !user.HasPermission(PermissionKind.EnableContentDownloading))
                {
                    throw new SecurityException("User does not have download access.");
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
                throw new AuthenticationException("Access token is required.");
            }

            var info = GetTokenInfo(request);

            if (info == null)
            {
                throw new AuthenticationException("Access token is invalid or expired.");
            }
        }
    }
}
