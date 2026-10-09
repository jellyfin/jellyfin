using System.Net;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Auth.LoopbackOrRequiresElevationPolicy
{
    /// <summary>
    /// Loopback access or require elevated privileges handler.
    /// </summary>
    public class LoopbackOrRequiresElevationHandler : AuthorizationHandler<LoopbackOrRequiresElevationRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoopbackOrRequiresElevationHandler"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        public LoopbackOrRequiresElevationHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LoopbackOrRequiresElevationRequirement requirement)
        {
            var ip = _httpContextAccessor.HttpContext?.GetNormalizedRemoteIP();

            // No HttpContext (e.g. in-process callers) is treated the same as loopback.
            if (ip is null || IPAddress.IsLoopback(ip))
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
