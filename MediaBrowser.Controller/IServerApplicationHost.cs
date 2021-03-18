#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.System;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller
{
    /// <summary>
    /// Interface IServerApplicationHost.
    /// </summary>
    public interface IServerApplicationHost : IApplicationHost
    {
        event EventHandler HasUpdateAvailableChanged;

        bool CoreStartupHasCompleted { get; }

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
        /// Gets the configured published server url.
        /// </summary>
        string PublishedServerUrl { get; }

        /// <summary>
        /// Gets the system info.
        /// </summary>
        /// <param name="source">The originator of the request.</param>
        /// <returns>SystemInfo.</returns>
        SystemInfo GetSystemInfo(IPAddress source);

        PublicSystemInfo GetPublicSystemInfo(IPAddress address);

        /// <summary>
        /// Gets a URL specific for the request.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> instance.</param>
        /// <param name="port">Optional port number.</param>
        /// <returns>An accessible URL.</returns>
        string GetSmartApiUrl(HttpRequest request, int? port = null);

        /// <summary>
        /// Gets a URL specific for the request.
        /// </summary>
        /// <param name="remoteAddr">The remote <see cref="IPAddress"/> of the connection.</param>
        /// <param name="port">Optional port number.</param>
        /// <returns>An accessible URL.</returns>
        string GetSmartApiUrl(IPAddress remoteAddr, int? port = null);

        /// <summary>
        /// Gets a URL specific for the request.
        /// </summary>
        /// <param name="hostname">The hostname used in the connection.</param>
        /// <param name="port">Optional port number.</param>
        /// <returns>An accessible URL.</returns>
        string GetSmartApiUrl(string hostname, int? port = null);

        /// <summary>
        /// Gets a localhost URL that can be used to access the API using the loop-back IP address.
        /// over HTTP (not HTTPS).
        /// </summary>
        /// <returns>The API URL.</returns>
        string GetLoopbackHttpApiUrl();

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

        /// <summary>
        /// Open a URL in an external browser window.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        /// <exception cref="NotSupportedException"><see cref="CanLaunchWebBrowser"/> is false.</exception>
        void LaunchUrl(string url);

        IEnumerable<WakeOnLanInfo> GetWakeOnLanInfo();

        string ExpandVirtualPath(string path);

        string ReverseVirtualPath(string path);
    }
}
