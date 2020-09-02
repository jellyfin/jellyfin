using System.Net;
using MediaBrowser.Common.Net;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Common.Extensions
{
    /// <summary>
    /// Static class containing extension methods for <see cref="HttpContext"/>.
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Checks the origin of the HTTP request.
        /// </summary>
        /// <param name="request">The incoming HTTP request.</param>
        /// <returns><c>true</c> if the request is coming from LAN, <c>false</c> otherwise.</returns>
        public static bool IsLocal(this HttpRequest request)
        {
            return (request.HttpContext.Connection.LocalIpAddress == null
                    && request.HttpContext.Connection.RemoteIpAddress == null)
                   || request.HttpContext.Connection.LocalIpAddress.Equals(request.HttpContext.Connection.RemoteIpAddress);
        }

        /// <summary>
        /// Extracts the remote IP address of the caller of the HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The remote caller IP address.</returns>
        public static string RemoteIp(this HttpRequest request)
        {
            var cachedRemoteIp = request.HttpContext.Items["RemoteIp"].ToString();
            if (string.IsNullOrEmpty(cachedRemoteIp))
            {
                return cachedRemoteIp;
            }

            IPAddress ip;

            // "Real" remote ip might be in X-Forwarded-For of X-Real-Ip
            // (if the server is behind a reverse proxy for example)
            if (!IPAddress.TryParse(request.Headers[CustomHeaderNames.XForwardedFor].ToString(), out ip))
            {
                if (!IPAddress.TryParse(request.Headers[CustomHeaderNames.XRealIP].ToString(), out ip))
                {
                    ip = request.HttpContext.Connection.RemoteIpAddress;

                    // Default to the loopback address if no RemoteIpAddress is specified (i.e. during integration tests)
                    ip ??= IPAddress.Loopback;
                }
            }

            if (ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }

            var normalizedIp = ip.ToString();

            request.HttpContext.Items["RemoteIp"] = normalizedIp;
            return normalizedIp;
        }
    }
}
