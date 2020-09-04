using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Networking;
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
            if (httpContext.Request.IsLocal())
            {
                await _next(httpContext).ConfigureAwait(false);
                return;
            }

            var remoteIp = httpContext.Request.RemoteIp();

            if (IPNetAddress.TryParse(remoteIp, out IPNetAddress remoteIPObj))
            {
                if (serverConfigurationManager.Configuration.EnableRemoteAccess)
                {
                    // Comma separated list of IP addresses or IP/netmask entries for networks that will be allowed to connect remotely.
                    // If left blank, all remote addresses will be allowed.
                    NetCollection remoteAddressFilter = networkManager.RemoteAddressFilter;

                    if (remoteAddressFilter.Count > 0 && !networkManager.IsInLocalNetwork(remoteIPObj))
                    {
                         // remoteAddressFilter is a whitelist or blacklist.
                         bool contained = remoteAddressFilter.Contains(remoteIPObj);
                         if (serverConfigurationManager.Configuration.IsRemoteIPFilterBlacklist)
                         {
                            if (!contained)
                            {
                                return;
                            }
                         }
                         else
                         {
                            if (contained)
                            {
                                return;
                            }
                         }
                    }
                }
                else
                {
                    if (networkManager.IsInLocalNetwork(remoteIPObj))
                    {
                        return;
                    }
                }
            }
            else
            {
                // _logger.LogError("Unable to parse remoteIp: {0}", remoteIp);
            }

            await _next(httpContext).ConfigureAwait(false);
        }
    }
}
