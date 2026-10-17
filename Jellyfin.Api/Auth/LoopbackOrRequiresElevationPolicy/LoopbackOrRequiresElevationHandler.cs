using System.Linq;
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
        // A reverse proxy on the server machine also connects from the loopback address, but adds one of these headers.
        private static readonly string[] _forwardingHeaders = ["Forwarded", "X-Forwarded-For", "X-Real-IP"];

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
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext is not null
                && IPAddress.IsLoopback(httpContext.GetNormalizedRemoteIP())
                && !_forwardingHeaders.Any(httpContext.Request.Headers.ContainsKey))
            {
                context.Succeed(requirement);
            }
            else if (context.User.IsInRole(UserRoles.Administrator))
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
