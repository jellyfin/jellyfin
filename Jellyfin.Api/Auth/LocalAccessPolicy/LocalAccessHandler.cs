using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;

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
        public LocalAccessHandler(IUserManager userManager, INetworkManager networkManager)
            : base(userManager, networkManager)
        {
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LocalAccessRequirement requirement)
        {
            var validated = ValidateClaims(context.User, localAccessOnly: true);
            if (!validated)
            {
                context.Fail();
            }
            else
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
