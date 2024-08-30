using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Auth.DefaultAuthorizationPolicy
{
    /// <summary>
    /// Default authorization handler.
    /// </summary>
    public class DefaultAuthorizationHandler : AuthorizationHandler<DefaultAuthorizationRequirement>
    {
        private readonly IUserManager _userManager;
        private readonly INetworkManager _networkManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAuthorizationHandler"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        public DefaultAuthorizationHandler(
            IUserManager userManager,
            INetworkManager networkManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _networkManager = networkManager;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DefaultAuthorizationRequirement requirement)
        {
            var isApiKey = context.User.GetIsApiKey();
            var userId = context.User.GetUserId();
            // This likely only happens during the wizard, so skip the default checks and let any other handlers do it
            if (!isApiKey && userId.IsEmpty())
            {
                return Task.CompletedTask;
            }

            if (isApiKey)
            {
                // Api keys are unrestricted.
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var isInLocalNetwork = _httpContextAccessor.HttpContext is not null
                                   && _networkManager.IsInLocalNetwork(_httpContextAccessor.HttpContext.GetNormalizedRemoteIP());
            var user = _userManager.GetUserById(userId);
            if (user is null)
            {
                throw new ResourceNotFoundException();
            }

            // User cannot access remotely and user is remote
            if (!isInLocalNetwork && !user.HasPermission(PermissionKind.EnableRemoteAccess))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            // Admins can do everything
            if (context.User.IsInRole(UserRoles.Administrator))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // It's not great to have this check, but parental schedule must usually be honored except in a few rare cases
            if (requirement.ValidateParentalSchedule && !user.IsParentalScheduleAllowed())
            {
                context.Fail();
                return Task.CompletedTask;
            }

            // Only succeed if the requirement isn't a subclass as any subclassed requirement will handle success in its own handler
            if (requirement.GetType() == typeof(DefaultAuthorizationRequirement))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
