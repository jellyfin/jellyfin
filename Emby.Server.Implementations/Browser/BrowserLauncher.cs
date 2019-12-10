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
        /// Opens the dashboard page.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="appHost">The app host.</param>
        private static void OpenDashboardPage(string page, IServerApplicationHost appHost)
        {
            var url = appHost.GetLocalApiUrl("localhost") + "/web/" + page;

            OpenUrl(appHost, url);
        }

        /// <summary>
        /// Opens the web client.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        public static void OpenWebApp(IServerApplicationHost appHost)
        {
            OpenDashboardPage("index.html", appHost);
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
