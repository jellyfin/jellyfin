using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Jellyfin.Networking.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Server.Middleware
{
    /// <summary>
    /// Validates the LAN host IP based on application configuration.
    /// </summary>
    public class LanFilteringMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="LanFilteringMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next delegate in the pipeline.</param>
        public LanFilteringMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Executes the middleware action.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <param name="networkManager">The network manager.</param>
        /// <param name="serverConfigurationManager">The server configuration manager.</param>
        /// <returns>The async task.</returns>
        public async Task Invoke(HttpContext httpContext, INetworkManager networkManager, IServerConfigurationManager serverConfigurationManager)
        {
            var host = httpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback;

            if (!networkManager.IsInLocalNetwork(host) && !serverConfigurationManager.GetNetworkConfiguration().EnableRemoteAccess)
            {
                return;
            }

            await _next(httpContext).ConfigureAwait(false);
        }
    }
}
