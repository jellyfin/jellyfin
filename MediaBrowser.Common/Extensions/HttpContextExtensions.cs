using System.Net;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Common.Extensions
{
    /// <summary>
    /// Static class containing extension methods for <see cref="HttpContext"/>.
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Checks the origin of the HTTP context.
        /// </summary>
        /// <param name="context">The incoming HTTP context.</param>
        /// <returns><c>true</c> if the request is coming from LAN, <c>false</c> otherwise.</returns>
        public static bool IsLocal(this HttpContext context)
        {
            return (context.Connection.LocalIpAddress is null
                    && context.Connection.RemoteIpAddress is null)
                   || Equals(context.Connection.LocalIpAddress, context.Connection.RemoteIpAddress);
        }

        /// <summary>
        /// Extracts the remote IP address of the caller of the HTTP context.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>The remote caller IP address.</returns>
        public static IPAddress GetNormalizedRemoteIP(this HttpContext context)
        {
            // Default to the loopback address if no RemoteIpAddress is specified (i.e. during integration tests)
            var ip = context.Connection.RemoteIpAddress ?? IPAddress.Loopback;

            if (ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }

            return ip;
        }
    }
}
