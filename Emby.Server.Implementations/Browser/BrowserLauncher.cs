using System;
using MediaBrowser.Controller;

namespace Emby.Server.Implementations.Browser
{
    /// <summary>
    /// Class BrowserLauncher.
    /// </summary>
    public static class BrowserLauncher
    {
        /// <summary>
        /// Opens the web client.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        public static void OpenWebApp(IServerApplicationHost appHost)
        {
            var url = appHost.GetLocalApiUrl("localhost") + "/web/index.html";
            OpenUrl(appHost, url);
        }

        /// <summary>
        /// Opens the swagger API page.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        public static void OpenSwaggerPage(IServerApplicationHost appHost)
        {
            var url = appHost.GetLocalApiUrl("localhost") + "/swagger/index.html";
            OpenUrl(appHost, url);
        }

        /// <summary>
        /// Opens the URL.
        /// </summary>
        /// <param name="appHost">The application host instance.</param>
        /// <param name="url">The URL.</param>
        private static void OpenUrl(IServerApplicationHost appHost, string url)
        {
            try
            {
                appHost.LaunchUrl(url);
            }
            catch (NotSupportedException)
            {

            }
            catch (Exception)
            {
            }
        }
    }
}
