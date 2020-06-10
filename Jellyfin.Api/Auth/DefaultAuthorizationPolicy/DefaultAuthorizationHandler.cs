using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.DefaultAuthorizationPolicy
{
    /// <summary>
    /// Default authorization handler.
    /// </summary>
    public class DefaultAuthorizationHandler : BaseAuthorizationHandler<DefaultAuthorizationRequirement>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAuthorizationHandler"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        public DefaultAuthorizationHandler(IUserManager userManager, INetworkManager networkManager)
            : base(userManager, networkManager)
        {
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DefaultAuthorizationRequirement requirement)
        {
            var validated = ValidateClaims(context.User);
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
