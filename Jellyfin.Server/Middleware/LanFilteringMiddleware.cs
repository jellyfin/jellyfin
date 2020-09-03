using System;
using System.Linq;
using System.Threading.Tasks;
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
            var currentHost = httpContext.Request.Host.ToString();
            var hosts = serverConfigurationManager
                .Configuration
                .LocalNetworkAddresses
                .Select(NormalizeConfiguredLocalAddress)
                .ToList();

            if (hosts.Count == 0)
            {
                await _next(httpContext).ConfigureAwait(false);
                return;
            }

            currentHost ??= string.Empty;

            if (networkManager.IsInPrivateAddressSpace(currentHost))
            {
                hosts.Add("localhost");
                hosts.Add("127.0.0.1");

                if (hosts.All(i => currentHost.IndexOf(i, StringComparison.OrdinalIgnoreCase) == -1))
                {
                    return;
                }
            }

            await _next(httpContext).ConfigureAwait(false);
        }

        private static string NormalizeConfiguredLocalAddress(string address)
        {
            var add = address.AsSpan().Trim('/');
            int index = add.IndexOf('/');
            if (index != -1)
            {
                add = add.Slice(index + 1);
            }

            return add.TrimStart('/').ToString();
        }
    }
}
