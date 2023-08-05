using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Auth.LocalAccessOrRequiresElevationPolicy
{
    /// <summary>
    /// Local access or require elevated privileges handler.
    /// </summary>
    public class LocalAccessOrRequiresElevationHandler : AuthorizationHandler<LocalAccessOrRequiresElevationRequirement>
    {
        private readonly INetworkManager _networkManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalAccessOrRequiresElevationHandler"/> class.
        /// </summary>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        public LocalAccessOrRequiresElevationHandler(
            INetworkManager networkManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _networkManager = networkManager;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LocalAccessOrRequiresElevationRequirement requirement)
        {
            var ip = _httpContextAccessor.HttpContext?.GetNormalizedRemoteIP();

            // Loopback will be on LAN, so we can accept null.
            if (ip is null || _networkManager.IsInLocalNetwork(ip))
            {
                context.Succeed(requirement);

                return Task.CompletedTask;
            }

            if (context.User.IsInRole(UserRoles.Administrator))
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
