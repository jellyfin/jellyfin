using System.Linq;
using System.Threading.Tasks;
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
                await _next(httpContext).ConfigureAwait(false);
                return;
            }

            var remoteIp = httpContext.GetNormalizedRemoteIp();

            if (serverConfigurationManager.Configuration.EnableRemoteAccess)
            {
                var addressFilter = serverConfigurationManager.Configuration.RemoteIPFilter.Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();

                if (addressFilter.Length > 0 && !networkManager.IsInLocalNetwork(remoteIp))
                {
                    if (serverConfigurationManager.Configuration.IsRemoteIPFilterBlacklist)
                    {
                        if (networkManager.IsAddressInSubnets(remoteIp, addressFilter))
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (!networkManager.IsAddressInSubnets(remoteIp, addressFilter))
                        {
                            return;
                        }
                    }
                }
            }
            else
            {
                if (!networkManager.IsInLocalNetwork(remoteIp))
                {
                    return;
                }
            }

            await _next(httpContext).ConfigureAwait(false);
        }
    }
}
