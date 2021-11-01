using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Auth.LocalAccessPolicy
{
    /// <summary>
    /// Local access handler.
    /// </summary>
    public class LocalAccessHandler : BaseAuthorizationHandler<LocalAccessRequirement>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocalAccessHandler"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        public LocalAccessHandler(
            IUserManager userManager,
            INetworkManager networkManager,
            IHttpContextAccessor httpContextAccessor)
            : base(userManager, networkManager, httpContextAccessor)
        {
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LocalAccessRequirement requirement)
        {
            var validated = ValidateClaims(context.User, localAccessOnly: true);
            if (validated)
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
