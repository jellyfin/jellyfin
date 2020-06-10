using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.IgnoreSchedulePolicy
{
    /// <summary>
    /// Escape schedule controls handler.
    /// </summary>
    public class IgnoreScheduleHandler : BaseAuthorizationHandler<IgnoreScheduleRequirement>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IgnoreScheduleHandler"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        public IgnoreScheduleHandler(IUserManager userManager, INetworkManager networkManager)
            : base(userManager, networkManager)
        {
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IgnoreScheduleRequirement requirement)
        {
            var validated = ValidateClaims(context.User, ignoreSchedule: true);
            if (!validated)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
