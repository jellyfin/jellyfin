using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Auth.DownloadPolicy
{
    /// <summary>
    /// Download authorization handler.
    /// </summary>
    public class DownloadHandler : BaseAuthorizationHandler<DownloadRequirement>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadHandler"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        public DownloadHandler(
            IUserManager userManager,
            INetworkManager networkManager,
            IHttpContextAccessor httpContextAccessor)
            : base(userManager, networkManager, httpContextAccessor)
        {
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DownloadRequirement requirement)
        {
            var validated = ValidateClaims(context.User);
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
