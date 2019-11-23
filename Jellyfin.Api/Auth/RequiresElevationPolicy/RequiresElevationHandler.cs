using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.RequiresElevationPolicy
{
    public class RequiresElevationHandler : AuthorizationHandler<RequiresElevationRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequiresElevationRequirement requirement)
        {
            if (context.User.IsInRole("Administrator"))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
