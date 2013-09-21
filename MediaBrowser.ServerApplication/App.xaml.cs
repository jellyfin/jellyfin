using MediaBrowser.Common.Events;
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
    public partial class App : Application
    {
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private readonly ILogger _logger;

        /// <summary>
        /// Gets or sets the composition root.
        /// </summary>
        /// <value>The composition root.</value>
        private readonly ApplicationHost _appHost;

        public event EventHandler AppStarted;

        public bool IsRunningAsService { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="App" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public App(ApplicationHost appHost, ILogger logger, bool isRunningAsService)
        {
            _appHost = appHost;
            _logger = logger;
            IsRunningAsService = isRunningAsService;

            InitializeComponent();
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
            _logger.ErrorException("UnhandledException", ex);

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
                if (!IsRunningAsService)
                {
                    ShowSplashWindow();
                }

                await _appHost.Init();

                if (!IsRunningAsService)
                {
                    HideSplashWindow();
                }

                var task = _appHost.RunStartupTasks();

                if (!IsRunningAsService)
                {
                    ShowMainWindow();
                }

                EventHelper.FireEventIfNotNull(AppStarted, this, EventArgs.Empty, _logger);

                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error launching application", ex);

                MessageBox.Show("There was an error launching Media Browser: " + ex.Message);

                // Shutdown the app with an error code
                Shutdown(1);
            }
        }

        private MainWindow _mainWindow;
        private void ShowMainWindow()
        {
            var host = _appHost;

            var win = new MainWindow(host.LogManager, host,
                                     host.ServerConfigurationManager, host.UserManager,
                                     host.LibraryManager, host.JsonSerializer,
                                     host.DisplayPreferencesRepository);

            win.Show();

            _mainWindow = win;
        }

        private void HideMainWindow()
        {
            if (_mainWindow != null)
            {
                _mainWindow.Hide();
                _mainWindow = null;
            }
        }

        private SplashWindow _splashWindow;
        private void ShowSplashWindow()
        {
            var win = new SplashWindow(_appHost.ApplicationVersion);
            win.Show();

            _splashWindow = win;
        }

        private void HideSplashWindow()
        {
            if (_splashWindow != null)
            {
                _splashWindow.Hide();
                _splashWindow = null;
            }
        }

        public void ShutdownApplication()
        {
            Dispatcher.Invoke(Shutdown);
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
    }
}
