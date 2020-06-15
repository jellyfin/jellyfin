using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

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
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        public IgnoreScheduleHandler(
            IUserManager userManager,
            INetworkManager networkManager,
            IHttpContextAccessor httpContextAccessor)
            : base(userManager, networkManager, httpContextAccessor)
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
