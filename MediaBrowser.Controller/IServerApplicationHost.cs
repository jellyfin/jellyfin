#nullable disable

#pragma warning disable CS1591

using System.Net;
using MediaBrowser.Common;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller
{
    /// <summary>
    /// Interface IServerApplicationHost.
    /// </summary>
    public interface IServerApplicationHost : IApplicationHost
    {
        bool CoreStartupHasCompleted { get; }

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
        /// Gets the name of the friendly.
        /// </summary>
        /// <value>The name of the friendly.</value>
        string FriendlyName { get; }

        /// <summary>
        /// Gets a URL specific for the request.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> instance.</param>
        /// <returns>An accessible URL.</returns>
        string GetSmartApiUrl(HttpRequest request);

        /// <summary>
        /// Gets a URL specific for the request.
        /// </summary>
        /// <param name="remoteAddr">The remote <see cref="IPAddress"/> of the connection.</param>
        /// <returns>An accessible URL.</returns>
        string GetSmartApiUrl(IPAddress remoteAddr);

        /// <summary>
        /// Gets a URL specific for the request.
        /// </summary>
        /// <param name="hostname">The hostname used in the connection.</param>
        /// <returns>An accessible URL.</returns>
        string GetSmartApiUrl(string hostname);

        /// <summary>
        /// Gets an URL that can be used to access the API over LAN.
        /// </summary>
        /// <param name="ipAddress">An optional IP address to use.</param>
        /// <param name="allowHttps">A value indicating whether to allow HTTPS.</param>
        /// <returns>The API URL.</returns>
        string GetApiUrlForLocalAccess(IPAddress ipAddress = null, bool allowHttps = true);

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
        string GetLocalApiUrl(string hostname, string scheme = null, int? port = null);

        string ExpandVirtualPath(string path);

        string ReverseVirtualPath(string path);
    }
}
