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
                var initProgress = new Progress<double>();

                if (!IsRunningAsService)
                {
                    ShowSplashWindow(initProgress);
                }

                await _appHost.Init(initProgress);

                var task = _appHost.RunStartupTasks();

                if (!IsRunningAsService)
                {
                    HideSplashWindow();
                }

                if (!IsRunningAsService)
                {
                    ShowMainWindow();
                }

                EventHelper.FireEventIfNotNull(AppStarted, this, EventArgs.Empty, _logger);

                await task;
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
                                     host.DisplayPreferencesRepository,
                                     host.ItemRepository);

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
        private void ShowSplashWindow(Progress<double> progress)
        {
            var win = new SplashWindow(_appHost.ApplicationVersion, progress);
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
    }
}
