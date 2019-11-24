using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.RequiresElevationPolicy
{
    /// <summary>
    /// Authorization handler for requiring elevated privileges.
    /// </summary>
    public class RequiresElevationHandler : AuthorizationHandler<RequiresElevationRequirement>
    {
        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequiresElevationRequirement requirement)
        {
            if (context.User.IsInRole(UserRoles.Administrator))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
