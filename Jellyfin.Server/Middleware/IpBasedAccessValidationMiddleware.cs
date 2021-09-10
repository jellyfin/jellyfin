using System.Net;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Server.Middleware
{
    /// <summary>
    /// Validates the IP of requests coming from local networks wrt. remote access.
    /// </summary>
    public class IpBasedAccessValidationMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="IpBasedAccessValidationMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next delegate in the pipeline.</param>
        public IpBasedAccessValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Executes the middleware action.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <param name="networkManager">The network manager.</param>
        /// <returns>The async task.</returns>
        public async Task Invoke(HttpContext httpContext, INetworkManager networkManager)
        {
            if (httpContext.IsLocal())
            {
                // Running locally.
                await _next(httpContext).ConfigureAwait(false);
                return;
            }

            var remoteIp = httpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback;

            if (!networkManager.HasRemoteAccess(remoteIp))
            {
                return;
            }

            await _next(httpContext).ConfigureAwait(false);
        }
    }
}
