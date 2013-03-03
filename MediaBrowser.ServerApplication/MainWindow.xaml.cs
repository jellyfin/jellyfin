using MediaBrowser.Common.Kernel;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.ServerApplication.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

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
        private readonly IApplicationHost _appHost;

        /// <summary>
        /// The _log manager
        /// </summary>
        private readonly ILogManager _logManager;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow" /> class.
        /// </summary>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="appHost">The app host.</param>
        /// <exception cref="System.ArgumentNullException">logger</exception>
        public MainWindow(ILogManager logManager, IApplicationHost appHost)
        {
            if (logManager == null)
            {
                throw new ArgumentNullException("logManager");
            }

            _logger = logManager.GetLogger("MainWindow");
            _appHost = appHost;
            _logManager = logManager;

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

            Instance_ConfigurationUpdated(null, EventArgs.Empty);

            _logManager.LoggerLoaded += LoadLogWindow;
            Kernel.Instance.ConfigurationUpdated += Instance_ConfigurationUpdated;
        }

        /// <summary>
        /// Handles the ConfigurationUpdated event of the Instance control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void Instance_ConfigurationUpdated(object sender, EventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                var developerToolsVisibility = Kernel.Instance.Configuration.EnableDeveloperTools
                                                   ? Visibility.Visible
                                                   : Visibility.Collapsed;

                separatorDeveloperTools.Visibility = developerToolsVisibility;
                cmdReloadServer.Visibility = developerToolsVisibility;
                cmOpenExplorer.Visibility = developerToolsVisibility;

                var logWindow = App.Instance.Windows.OfType<LogWindow>().FirstOrDefault();

                if ((logWindow == null && Kernel.Instance.Configuration.ShowLogWindow) || (logWindow != null && !Kernel.Instance.Configuration.ShowLogWindow))
                {
                    _logManager.ReloadLogger(Kernel.Instance.Configuration.EnableDebugLevelLogging ? LogSeverity.Debug : LogSeverity.Info);
                }
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
                if (Kernel.Instance.Configuration.ShowLogWindow)
                {
                    Trace.Listeners.Add(new WindowTraceListener(new LogWindow(Kernel.Instance)));
                }
                else
                {
                    Trace.Listeners.Remove("MBLogWindow");
                }
                // Set menu option indicator
                cmShowLogWindow.IsChecked = Kernel.Instance.Configuration.ShowLogWindow;

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
            App.OpenUrl("http://localhost:" + Kernel.Instance.Configuration.HttpServerPortNumber + "/" +
                      Kernel.Instance.WebApplicationName + "/metadata");
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
            var explorer = (LibraryExplorer)_appHost.CreateInstance(typeof(LibraryExplorer));
            explorer.Show();
        }

        /// <summary>
        /// Handles the click event of the cmOpenDashboard control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void cmOpenDashboard_click(object sender, RoutedEventArgs e)
        {
            var user = _appHost.Resolve<IUserManager>().Users.FirstOrDefault(u => u.Configuration.IsAdministrator);
            App.OpenDashboard(user);
        }

        /// <summary>
        /// Handles the click event of the cmVisitCT control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void cmVisitCT_click(object sender, RoutedEventArgs e)
        {
            App.OpenUrl("http://community.mediabrowser.tv/");
        }

        /// <summary>
        /// Handles the click event of the cmdBrowseLibrary control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void cmdBrowseLibrary_click(object sender, RoutedEventArgs e)
        {
            var user = _appHost.Resolve<IUserManager>().Users.FirstOrDefault(u => u.Configuration.IsAdministrator);
            App.OpenDashboardPage("index.html", user);
        }

        /// <summary>
        /// Handles the click event of the cmExit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void cmExit_click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Handles the click event of the cmdReloadServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void cmdReloadServer_click(object sender, RoutedEventArgs e)
        {
            App.Instance.Restart();
        }

        /// <summary>
        /// Handles the click event of the CmShowLogWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void CmShowLogWindow_click(object sender, RoutedEventArgs e)
        {
            Kernel.Instance.Configuration.ShowLogWindow = !Kernel.Instance.Configuration.ShowLogWindow;
            Kernel.Instance.SaveConfiguration();
            LoadLogWindow(sender, e);
        }

        #endregion
    }
}
