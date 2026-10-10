using System.Net;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Auth.LoopbackLibraryRefreshOrRequiresElevationPolicy
{
    /// <summary>
    /// Loopback library refresh access, if enabled, or require elevated privileges handler.
    /// </summary>
    public class LoopbackLibraryRefreshOrRequiresElevationHandler : AuthorizationHandler<LoopbackLibraryRefreshOrRequiresElevationRequirement>
    {
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoopbackLibraryRefreshOrRequiresElevationHandler"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        public LoopbackLibraryRefreshOrRequiresElevationHandler(
            IServerConfigurationManager serverConfigurationManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _serverConfigurationManager = serverConfigurationManager;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LoopbackLibraryRefreshOrRequiresElevationRequirement requirement)
        {
            var ip = _httpContextAccessor.HttpContext?.GetNormalizedRemoteIP();

            // No HttpContext (e.g. in-process callers) is treated the same as loopback.
            if (_serverConfigurationManager.Configuration.AllowLoopbackLibraryRefresh
                && (ip is null || IPAddress.IsLoopback(ip)))
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
