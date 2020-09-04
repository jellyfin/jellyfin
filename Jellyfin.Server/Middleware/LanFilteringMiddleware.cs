using System;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Networking;
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
            var currentHost = httpContext.Request.Host.ToString() ?? string.Empty;

            if (IPHost.TryParse(currentHost, out IPHost h))
            {
                if (h.HasAddress)
                {
                    if (!networkManager.IsInLocalNetwork(h))
                    {
                        return;
                    }
                }
                else
                {
                    // Host is not an IP address.
                    // Can we make Assumption is that host names are not local.
                    // Could attempt resolve, but do we want to do this on each request?
                    if (!serverConfigurationManager.Configuration.EnableRemoteAccess)
                    {
                        return;
                    }
                }
            }

            await _next(httpContext).ConfigureAwait(false);
        }
    }
}
