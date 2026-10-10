using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Auth.LoopbackAccessPolicy
{
    /// <summary>
    /// Loopback access handler, only allowing requests made from the server machine itself.
    /// </summary>
    public class LoopbackAccessHandler : AuthorizationHandler<LoopbackAccessRequirement>
    {
        // A reverse proxy on the server machine also connects from the loopback address, but adds one of these headers.
        private static readonly string[] _forwardingHeaders = ["Forwarded", "X-Forwarded-For", "X-Real-IP"];

        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoopbackAccessHandler"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        public LoopbackAccessHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LoopbackAccessRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext is not null
                && IPAddress.IsLoopback(httpContext.GetNormalizedRemoteIP())
                && !_forwardingHeaders.Any(httpContext.Request.Headers.ContainsKey))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
