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
        /// <param name="serverConfigurationManager">The server configuration manager.</param>
        /// <returns>The async task.</returns>
        public async Task Invoke(HttpContext httpContext, INetworkManager networkManager, IServerConfigurationManager serverConfigurationManager)
        {
            if (httpContext.IsLocal())
            {
                // Running locally.
                await _next(httpContext).ConfigureAwait(false);
                return;
            }

            var remoteIp = httpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback;

            if (serverConfigurationManager.GetNetworkConfiguration().EnableRemoteAccess)
            {
                // Comma separated list of IP addresses or IP/netmask entries for networks that will be allowed to connect remotely.
                // If left blank, all remote addresses will be allowed.
                var remoteAddressFilter = networkManager.RemoteAddressFilter;

                if (remoteAddressFilter.Count > 0 && !networkManager.IsInLocalNetwork(remoteIp))
                {
                    // remoteAddressFilter is a whitelist or blacklist.
                    bool isListed = remoteAddressFilter.ContainsAddress(remoteIp);
                    if (!serverConfigurationManager.GetNetworkConfiguration().IsRemoteIPFilterBlacklist)
                    {
                        // Black list, so flip over.
                        isListed = !isListed;
                    }

                    if (!isListed)
                    {
                        // If your name isn't on the list, you arn't coming in.
                        return;
                    }
                }
            }
            else if (!networkManager.IsInLocalNetwork(remoteIp))
            {
                // Remote not enabled. So everyone should be LAN.
                return;
            }

            await _next(httpContext).ConfigureAwait(false);
        }
    }
}
