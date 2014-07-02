using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System;
using System.Diagnostics;
using System.Linq;

namespace MediaBrowser.ServerApplication.Native
{
    /// <summary>
    /// Class BrowserLauncher
    /// </summary>
    public static class BrowserLauncher
    {
        /// <summary>
        /// Opens the dashboard page.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="loggedInUser">The logged in user.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="appHost">The app host.</param>
        /// <param name="logger">The logger.</param>
        public static void OpenDashboardPage(string page, User loggedInUser, IServerConfigurationManager configurationManager, IServerApplicationHost appHost, ILogger logger)
        {
            var url = "http://localhost:" + configurationManager.Configuration.HttpServerPortNumber + "/" +
                      appHost.WebApplicationName + "/web/" + page;

            OpenUrl(url, logger);
        }

        /// <summary>
        /// Opens the github.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public static void OpenGithub(ILogger logger)
        {
            OpenUrl("https://github.com/MediaBrowser/MediaBrowser", logger);
        }

        /// <summary>
        /// Opens the community.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public static void OpenCommunity(ILogger logger)
        {
            OpenUrl("http://mediabrowser.tv/community", logger);
        }

        /// <summary>
        /// Opens the web client.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="appHost">The app host.</param>
        /// <param name="logger">The logger.</param>
        public static void OpenWebClient(IUserManager userManager, IServerConfigurationManager configurationManager, IServerApplicationHost appHost, ILogger logger)
        {
            var user = userManager.Users.FirstOrDefault(u => u.Configuration.IsAdministrator);
            OpenDashboardPage("index.html", user, configurationManager, appHost, logger);
        }

        /// <summary>
        /// Opens the dashboard.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="appHost">The app host.</param>
        /// <param name="logger">The logger.</param>
        public static void OpenDashboard(IUserManager userManager, IServerConfigurationManager configurationManager, IServerApplicationHost appHost, ILogger logger)
        {
            var user = userManager.Users.FirstOrDefault(u => u.Configuration.IsAdministrator);
            OpenDashboardPage("dashboard.html", user, configurationManager, appHost, logger);
        }

        /// <summary>
        /// Opens the swagger.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="appHost">The app host.</param>
        /// <param name="logger">The logger.</param>
        public static void OpenSwagger(IServerConfigurationManager configurationManager, IServerApplicationHost appHost, ILogger logger)
        {
            OpenUrl("http://localhost:" + configurationManager.Configuration.HttpServerPortNumber + "/" +
                      appHost.WebApplicationName + "/swagger-ui/index.html", logger);
        }

        /// <summary>
        /// Opens the standard API documentation.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="appHost">The app host.</param>
        /// <param name="logger">The logger.</param>
        public static void OpenStandardApiDocumentation(IServerConfigurationManager configurationManager, IServerApplicationHost appHost, ILogger logger)
        {
            OpenUrl("http://localhost:" + configurationManager.Configuration.HttpServerPortNumber + "/" +
                      appHost.WebApplicationName + "/metadata", logger);
        }

        /// <summary>
        /// Opens the URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="logger">The logger.</param>
        private static void OpenUrl(string url, ILogger logger)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = url
                },

                EnableRaisingEvents = true,
            };

            process.Exited += ProcessExited;

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error launching url: {0}", ex, url);

                Console.WriteLine("Error launching browser");
                Console.WriteLine(ex.Message);

#if !__MonoCS__
                System.Windows.Forms.MessageBox.Show("There was an error launching your web browser. Please check your default browser settings.");
#endif
            }
        }

        /// <summary>
        /// Processes the exited.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private static void ProcessExited(object sender, EventArgs e)
        {
            ((Process)sender).Dispose();
        }
    }
}
