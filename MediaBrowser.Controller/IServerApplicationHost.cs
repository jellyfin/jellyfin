using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Model.System;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller
{
    /// <summary>
    /// Interface IServerApplicationHost
    /// </summary>
    public interface IServerApplicationHost : IApplicationHost
    {
        event EventHandler HasUpdateAvailableChanged;

        /// <summary>
        /// Gets the system info.
        /// </summary>
        /// <returns>SystemInfo.</returns>
        Task<SystemInfo> GetSystemInfo(CancellationToken cancellationToken);

        Task<PublicSystemInfo> GetPublicSystemInfo(CancellationToken cancellationToken);

        bool CanLaunchWebBrowser { get; }

        /// <summary>
        /// Gets the HTTP server port.
        /// </summary>
        /// <value>The HTTP server port.</value>
        int HttpPort { get; }

        /// <summary>
        /// Gets the HTTPS port.
        /// </summary>
        /// <value>The HTTPS port.</value>
        int HttpsPort { get; }

        /// <summary>
        /// Gets a value indicating whether the server should listen on an HTTPS port.
        /// </summary>
        bool ListenWithHttps { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has update available.
        /// </summary>
        /// <value><c>true</c> if this instance has update available; otherwise, <c>false</c>.</value>
        bool HasUpdateAvailable { get; }

        /// <summary>
        /// Gets the name of the friendly.
        /// </summary>
        /// <value>The name of the friendly.</value>
        string FriendlyName { get; }

        /// <summary>
        /// Gets all the local IP addresses of this API instance. Each address is validated by sending a 'ping' request
        /// to the API that should exist at the address.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the task.</param>
        /// <returns>A list containing all the local IP addresses of the server.</returns>
        Task<List<IPAddress>> GetLocalIpAddresses(CancellationToken cancellationToken);

        /// <summary>
        /// Gets a local (LAN) URL that can be used to access the API. The hostname used is the first valid configured
        /// IP address that can be found via <see cref="GetLocalIpAddresses"/>. HTTPS will be preferred when available.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the task.</param>
        /// <returns>The server URL.</returns>
        Task<string> GetLocalApiUrl(CancellationToken cancellationToken);

        /// <summary>
        /// Gets a localhost URL that can be used to access the API using the loop-back IP address (127.0.0.1)
        /// over HTTP (not HTTPS).
        /// </summary>
        /// <returns>The API URL.</returns>
        string GetLoopbackHttpApiUrl();

        /// <summary>
        /// Gets a local (LAN) URL that can be used to access the API. HTTPS will be preferred when available.
        /// </summary>
        /// <param name="address">The IP address to use as the hostname in the URL.</param>
        /// <returns>The API URL.</returns>
        string GetLocalApiUrl(IPAddress address);

        /// <summary>
        /// Gets a local (LAN) URL that can be used to access the API.
        /// Note: if passing non-null scheme or port it is up to the caller to ensure they form the correct pair.
        /// </summary>
        /// <param name="hostname">The hostname to use in the URL.</param>
        /// <param name="scheme">
        /// The scheme to use for the URL. If null, the scheme will be selected automatically,
        /// preferring HTTPS, if available.
        /// </param>
        /// <param name="port">
        /// The port to use for the URL. If null, the port will be selected automatically,
        /// preferring the HTTPS port, if available.
        /// </param>
        /// <returns>The API URL.</returns>
        string GetLocalApiUrl(ReadOnlySpan<char> hostname, string scheme = null, int? port = null);

        /// <summary>
        /// Open a URL in an external browser window.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        /// <exception cref="NotSupportedException"><see cref="CanLaunchWebBrowser"/> is false.</exception>
        void LaunchUrl(string url);

        void EnableLoopback(string appName);

        IEnumerable<WakeOnLanInfo> GetWakeOnLanInfo();

        string ExpandVirtualPath(string path);
        string ReverseVirtualPath(string path);

        Task ExecuteHttpHandlerAsync(HttpContext context, Func<Task> next);
    }
}
