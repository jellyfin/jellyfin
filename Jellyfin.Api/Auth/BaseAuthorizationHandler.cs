using System.Security.Claims;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Auth
{
    /// <summary>
    /// Base authorization handler.
    /// </summary>
    /// <typeparam name="T">Type of Authorization Requirement.</typeparam>
    public abstract class BaseAuthorizationHandler<T> : AuthorizationHandler<T>
        where T : IAuthorizationRequirement
    {
        private readonly IUserManager _userManager;
        private readonly INetworkManager _networkManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseAuthorizationHandler{T}"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        protected BaseAuthorizationHandler(
            IUserManager userManager,
            INetworkManager networkManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _networkManager = networkManager;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Validate authenticated claims.
        /// </summary>
        /// <param name="claimsPrincipal">Request claims.</param>
        /// <param name="ignoreSchedule">Whether to ignore parental control.</param>
        /// <param name="localAccessOnly">Whether access is to be allowed locally only.</param>
        /// <param name="requiredDownloadPermission">Whether validation requires download permission.</param>
        /// <returns>Validated claim status.</returns>
        protected bool ValidateClaims(
            ClaimsPrincipal claimsPrincipal,
            bool ignoreSchedule = false,
            bool localAccessOnly = false,
            bool requiredDownloadPermission = false)
        {
            // ApiKey is currently global admin, always allow.
            var isApiKey = claimsPrincipal.GetIsApiKey();
            if (isApiKey)
            {
                return true;
            }

            // Ensure claim has userId.
            var userId = claimsPrincipal.GetUserId();
            if (userId.Equals(default))
            {
                return false;
            }

            // Ensure userId links to a valid user.
            var user = _userManager.GetUserById(userId);
            if (user == null)
            {
                return false;
            }

            // Ensure user is not disabled.
            if (user.HasPermission(PermissionKind.IsDisabled))
            {
                return false;
            }

            var isInLocalNetwork = _httpContextAccessor.HttpContext != null
                && _networkManager.IsInLocalNetwork(_httpContextAccessor.HttpContext.GetNormalizedRemoteIp());

            // User cannot access remotely and user is remote
            if (!user.HasPermission(PermissionKind.EnableRemoteAccess) && !isInLocalNetwork)
            {
                return false;
            }

            if (localAccessOnly && !isInLocalNetwork)
            {
                return false;
            }

            // User attempting to access out of parental control hours.
            if (!ignoreSchedule
                && !user.HasPermission(PermissionKind.IsAdministrator)
                && !user.IsParentalScheduleAllowed())
            {
                return false;
            }

            // User attempting to download without permission.
            if (requiredDownloadPermission
                && !user.HasPermission(PermissionKind.EnableContentDownloading))
            {
                return false;
            }

            return true;
        }
    }
}
