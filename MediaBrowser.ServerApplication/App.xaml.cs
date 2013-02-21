using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.UI;
using MediaBrowser.Controller;
using MediaBrowser.IsoMounter;
using MediaBrowser.Server.Uninstall;
using MediaBrowser.ServerApplication.Implementations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : BaseApplication, IApplication
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            RunApplication<App>("MediaBrowserServer");
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static App Instance
        {
            get
            {
                return Current as App;
            }
        }

        /// <summary>
        /// Gets the name of the product.
        /// </summary>
        /// <value>The name of the product.</value>
        protected override string ProductName
        {
            get { return Globals.ProductName; }
        }

        /// <summary>
        /// Gets the name of the publisher.
        /// </summary>
        /// <value>The name of the publisher.</value>
        protected override string PublisherName
        {
            get { return Globals.PublisherName; }
        }

        /// <summary>
        /// Gets the name of the suite.
        /// </summary>
        /// <value>The name of the suite.</value>
        protected override string SuiteName
        {
            get { return Globals.SuiteName; }
        }

        /// <summary>
        /// Gets the name of the uninstaller file.
        /// </summary>
        /// <value>The name of the uninstaller file.</value>
        protected override string UninstallerFileName
        {
            get { return "MediaBrowser.Server.Uninstall.exe"; }
        }

        /// <summary>
        /// Called when [second instance launched].
        /// </summary>
        /// <param name="args">The args.</param>
        protected override void OnSecondInstanceLaunched(IList<string> args)
        {
            base.OnSecondInstanceLaunched(args);

            OpenDashboard();
            InitializeComponent();
        }

        /// <summary>
        /// Opens the dashboard.
        /// </summary>
        public static void OpenDashboard()
        {
            OpenDashboardPage("dashboard.html");
        }

        /// <summary>
        /// Opens the dashboard page.
        /// </summary>
        /// <param name="page">The page.</param>
        public static void OpenDashboardPage(string page)
        {
            var url = "http://localhost:" + Controller.Kernel.Instance.Configuration.HttpServerPortNumber + "/" +
                      Controller.Kernel.Instance.WebApplicationName + "/dashboard/" + page;

            url = AddAutoLoginToDashboardUrl(url);

            OpenUrl(url);
        }

        /// <summary>
        /// Adds the auto login to dashboard URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.String.</returns>
        public static string AddAutoLoginToDashboardUrl(string url)
        {
            var user = Controller.Kernel.Instance.Users.FirstOrDefault(u => u.Configuration.IsAdministrator);

            if (user != null)
            {
                if (url.IndexOf('?') == -1)
                {
                    url += "?u=" + user.Id;
                }
                else
                {
                    url += "&u=" + user.Id;
                }
            }

            return url;
        }

        /// <summary>
        /// Opens the URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        public static void OpenUrl(string url)
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

            process.Start();
        }

        /// <summary>
        /// Processes the exited.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        static void ProcessExited(object sender, EventArgs e)
        {
            ((Process)sender).Dispose();
        }

        /// <summary>
        /// Instantiates the kernel.
        /// </summary>
        /// <returns>IKernel.</returns>
        protected override IKernel InstantiateKernel()
        {
            return new Kernel(new PismoIsoManager(LogManager.GetLogger("PismoIsoManager")), new DotNetZipClient());
        }

        /// <summary>
        /// Instantiates the main window.
        /// </summary>
        /// <returns>Window.</returns>
        protected override Window InstantiateMainWindow()
        {
            return new MainWindow();
        }
    }
}
