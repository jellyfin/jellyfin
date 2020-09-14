using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Auth.RequiresElevationPolicy
{
    /// <summary>
    /// Authorization handler for requiring elevated privileges.
    /// </summary>
    public class RequiresElevationHandler : BaseAuthorizationHandler<RequiresElevationRequirement>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequiresElevationHandler"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        public RequiresElevationHandler(
            IUserManager userManager,
            INetworkManager networkManager,
            IHttpContextAccessor httpContextAccessor)
            : base(userManager, networkManager, httpContextAccessor)
        {
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequiresElevationRequirement requirement)
        {
            var validated = ValidateClaims(context.User);
            if (validated && context.User.IsInRole(UserRoles.Administrator))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}
