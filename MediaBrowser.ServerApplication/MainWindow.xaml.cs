using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.ServerApplication.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using MediaBrowser.ServerApplication.Native;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _app host
        /// </summary>
        private readonly IServerApplicationHost _appHost;

        /// <summary>
        /// The _log manager
        /// </summary>
        private readonly ILogManager _logManager;

        /// <summary>
        /// The _configuration manager
        /// </summary>
        private readonly IServerConfigurationManager _configurationManager;

        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IDisplayPreferencesRepository _displayPreferencesManager;
        private readonly IItemRepository _itemRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow" /> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <param name="appHost">The app host.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="displayPreferencesManager">The display preferences manager.</param>
        /// <exception cref="System.ArgumentNullException">logger</exception>
        public MainWindow(ILogManager logManager, IServerApplicationHost appHost, IServerConfigurationManager configurationManager, IUserManager userManager, ILibraryManager libraryManager, IJsonSerializer jsonSerializer, IDisplayPreferencesRepository displayPreferencesManager, IItemRepository itemRepo)
        {
            if (logManager == null)
            {
                throw new ArgumentNullException("logManager");
            }
            if (appHost == null)
            {
                throw new ArgumentNullException("appHost");
            }
            if (configurationManager == null)
            {
                throw new ArgumentNullException("configurationManager");
            }

            _logger = logManager.GetLogger("MainWindow");
            _itemRepository = itemRepo;
            _appHost = appHost;
            _logManager = logManager;
            _configurationManager = configurationManager;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _jsonSerializer = jsonSerializer;
            _displayPreferencesManager = displayPreferencesManager;

            InitializeComponent();

            Loaded += MainWindowLoaded;
        }

        /// <summary>
        /// Mains the window loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;

            UpdateButtons();

            LoadLogWindow(null, EventArgs.Empty);
            _logManager.LoggerLoaded += LoadLogWindow;
            _configurationManager.ConfigurationUpdated += Instance_ConfigurationUpdated;

            if (_appHost.IsFirstRun)
            {
                Dispatcher.InvokeAsync(() => MbTaskbarIcon.ShowBalloonTip("Media Browser", "Welcome to Media Browser Server!", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.None));
            }
        }

        /// <summary>
        /// Handles the ConfigurationUpdated event of the Instance control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void Instance_ConfigurationUpdated(object sender, EventArgs e)
        {
            UpdateButtons();

            Dispatcher.InvokeAsync(() =>
            {
                var logWindow = App.Current.Windows.OfType<LogWindow>().FirstOrDefault();

                if ((logWindow == null && _configurationManager.Configuration.ShowLogWindow) || (logWindow != null && !_configurationManager.Configuration.ShowLogWindow))
                {
                    _logManager.ReloadLogger(_configurationManager.Configuration.EnableDebugLevelLogging ? LogSeverity.Debug : LogSeverity.Info);
                }
            });
        }

        private void UpdateButtons()
        {
            Dispatcher.InvokeAsync(() =>
            {
                var developerToolsVisibility = _configurationManager.Configuration.EnableDeveloperTools
                                                   ? Visibility.Visible
                                                   : Visibility.Collapsed;

                separatorDeveloperTools.Visibility = developerToolsVisibility;
                cmdReloadServer.Visibility = developerToolsVisibility;
                cmOpenExplorer.Visibility = developerToolsVisibility;
                cmShowLogWindow.Visibility = developerToolsVisibility;
            });
        }

        /// <summary>
        /// Loads the log window.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        void LoadLogWindow(object sender, EventArgs args)
        {
            CloseLogWindow();

            Dispatcher.InvokeAsync(() =>
            {
                // Add our log window if specified
                if (_configurationManager.Configuration.ShowLogWindow)
                {
                    Trace.Listeners.Add(new WindowTraceListener(new LogWindow(_logManager)));
                }
                else
                {
                    Trace.Listeners.Remove("MBLogWindow");
                }
                // Set menu option indicator
                cmShowLogWindow.IsChecked = _configurationManager.Configuration.ShowLogWindow;

            }, DispatcherPriority.Normal);
        }

        /// <summary>
        /// Closes the log window.
        /// </summary>
        void CloseLogWindow()
        {
            Dispatcher.InvokeAsync(() =>
            {
                foreach (var win in Application.Current.Windows.OfType<LogWindow>())
                {
                    win.Close();
                }
            });
        }

        /// <summary>
        /// Handles the Click event of the cmdApiDocs control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void cmdApiDocs_Click(object sender, EventArgs e)
        {
            BrowserLauncher.OpenStandardApiDocumentation(_configurationManager, _appHost, _logger);
        }

        void cmdSwaggerApiDocs_Click(object sender, EventArgs e)
        {
            BrowserLauncher.OpenSwagger(_configurationManager, _appHost, _logger);
        }

        void cmdGithubWiki_Click(object sender, EventArgs e)
        {
            BrowserLauncher.OpenGithub(_logger);
        }

        /// <summary>
        /// Occurs when [property changed].
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="info">The info.</param>
        public void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                try
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(info));
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in event handler", ex);
                }
            }
        }

        #region Context Menu events
        /// <summary>
        /// Handles the click event of the cmOpenExplorer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void cmOpenExplorer_click(object sender, RoutedEventArgs e)
        {
            new LibraryExplorer(_jsonSerializer, _logger, _appHost, _userManager, _libraryManager, _displayPreferencesManager, _itemRepository).Show();
        }

        /// <summary>
        /// Handles the click event of the cmOpenDashboard control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void cmOpenDashboard_click(object sender, RoutedEventArgs e)
        {
            BrowserLauncher.OpenDashboard(_userManager, _configurationManager, _appHost, _logger);
        }

        /// <summary>
        /// Handles the click event of the cmVisitCT control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void cmVisitCT_click(object sender, RoutedEventArgs e)
        {
            BrowserLauncher.OpenCommunity(_logger);
        }

        /// <summary>
        /// Handles the click event of the cmdBrowseLibrary control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void cmdBrowseLibrary_click(object sender, RoutedEventArgs e)
        {
            BrowserLauncher.OpenWebClient(_userManager, _configurationManager, _appHost, _logger);
        }

        /// <summary>
        /// Handles the click event of the cmExit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private async void cmExit_click(object sender, RoutedEventArgs e)
        {
            await _appHost.Shutdown().ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the click event of the cmdReloadServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private async void cmdReloadServer_click(object sender, RoutedEventArgs e)
        {
            await _appHost.Restart().ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the click event of the CmShowLogWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void CmShowLogWindow_click(object sender, RoutedEventArgs e)
        {
            _configurationManager.Configuration.ShowLogWindow = !_configurationManager.Configuration.ShowLogWindow;
            _configurationManager.SaveConfiguration();
            LoadLogWindow(sender, e);
        }

        #endregion
    }
}
