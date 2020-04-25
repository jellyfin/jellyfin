using System;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Browser
{
    /// <summary>
    /// Assists in opening application URLs in an external browser.
    /// </summary>
    public static class BrowserLauncher
    {
        /// <summary>
        /// Opens the home page of the web client.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        public static void OpenWebApp(IServerApplicationHost appHost)
        {
            TryOpenUrl(appHost, "/web/index.html");
        }

        /// <summary>
        /// Opens the swagger API page.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        public static void OpenSwaggerPage(IServerApplicationHost appHost)
        {
            TryOpenUrl(appHost, "/api-docs/v1/swagger");
        }

        /// <summary>
        /// Opens the specified URL in an external browser window. Any exceptions will be logged, but ignored.
        /// </summary>
        /// <param name="appHost">The application host.</param>
        /// <param name="url">The URL.</param>
        private static void TryOpenUrl(IServerApplicationHost appHost, string url)
        {
            try
            {
                string baseUrl = appHost.GetLocalApiUrl("localhost");
                appHost.LaunchUrl(baseUrl + url);
            }
            catch (Exception ex)
            {
                var logger = appHost.Resolve<ILogger>();
                logger?.LogError(ex, "Failed to open browser window with URL {URL}", url);
            }
        }
    }
}
