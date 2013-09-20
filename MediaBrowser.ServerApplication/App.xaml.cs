using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.ServerApplication.Splash;
using System;
using System.Diagnostics;
using System.Windows;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IApplicationInterface
    {
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the composition root.
        /// </summary>
        /// <value>The composition root.</value>
        protected ApplicationHost CompositionRoot { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="App" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public App()
        {
            InitializeComponent();
        }

        public bool IsBackgroundService
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the name of the uninstaller file.
        /// </summary>
        /// <value>The name of the uninstaller file.</value>
        protected string UninstallerFileName
        {
            get { return "MediaBrowser.Server.Uninstall.exe"; }
        }

        public void OnUnhandledException(Exception ex)
        {
            Logger.ErrorException("UnhandledException", ex);

            MessageBox.Show("Unhandled exception: " + ex.Message);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LoadApplication();
        }

        /// <summary>
        /// Loads the kernel.
        /// </summary>
        protected async void LoadApplication()
        {
            try
            {
                CompositionRoot = new ApplicationHost(this);

                Logger = CompositionRoot.LogManager.GetLogger("App");

                var splash = new SplashWindow(CompositionRoot.ApplicationVersion);

                splash.Show();

                await CompositionRoot.Init();

                splash.Hide();

                var task = CompositionRoot.RunStartupTasks();

                new MainWindow(CompositionRoot.LogManager, CompositionRoot, CompositionRoot.ServerConfigurationManager, CompositionRoot.UserManager, CompositionRoot.LibraryManager, CompositionRoot.JsonSerializer, CompositionRoot.DisplayPreferencesRepository).Show();

                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error launching application", ex);

                MessageBox.Show("There was an error launching Media Browser: " + ex.Message);

                // Shutdown the app with an error code
                Shutdown(1);
            }
        }

        public void ShutdownApplication()
        {
            Dispatcher.Invoke(Shutdown);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Application.Exit" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.Windows.ExitEventArgs" /> that contains the event data.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            MainStartup.ReleaseMutex();

            base.OnExit(e);

            if (CompositionRoot != null)
            {
                CompositionRoot.Dispose();
            }
        }

        /// <summary>
        /// Opens the dashboard page.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="loggedInUser">The logged in user.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="appHost">The app host.</param>
        public static void OpenDashboardPage(string page, User loggedInUser, IServerConfigurationManager configurationManager, IServerApplicationHost appHost)
        {
            var url = "http://localhost:" + configurationManager.Configuration.HttpServerPortNumber + "/" +
                      appHost.WebApplicationName + "/dashboard/" + page;

            OpenUrl(url);
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

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error launching your web browser. Please check your defualt browser settings.");
            }
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
        /// Restarts this instance.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void RestartApplication()
        {
            Dispatcher.Invoke(MainStartup.ReleaseMutex);

            CompositionRoot.Dispose();

            System.Windows.Forms.Application.Restart();

            Dispatcher.Invoke(Shutdown);
        }
    }
}
