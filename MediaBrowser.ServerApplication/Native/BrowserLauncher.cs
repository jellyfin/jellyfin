using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace MediaBrowser.ServerApplication.Native
{
    public static class BrowserLauncher
    {
        /// <summary>
        /// Opens the dashboard page.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="loggedInUser">The logged in user.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="appHost">The app host.</param>
        public static void OpenDashboardPage(string page, User loggedInUser, IServerConfigurationManager configurationManager, IServerApplicationHost appHost, ILogger logger)
        {
            var url = "http://localhost:" + configurationManager.Configuration.HttpServerPortNumber + "/" +
                      appHost.WebApplicationName + "/dashboard/" + page;

            OpenUrl(url, logger);
        }

        /// <summary>
        /// Opens the URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        public static void OpenUrl(string url, ILogger logger)
        {
            var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                        {
                            FileName = url
                        },

                    EnableRaisingEvents = true
                };

            process.Exited += ProcessExited;

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error launching url: {0}", ex, url);

                MessageBox.Show("There was an error launching your web browser. Please check your default browser settings.");
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
