using MediaBrowser.Common.Kernel;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.ServerApplication.Controls;
using MediaBrowser.ServerApplication.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Holds the list of new items to display when the NewItemTimer expires
        /// </summary>
        private readonly List<BaseItem> _newlyAddedItems = new List<BaseItem>();

        /// <summary>
        /// The amount of time to wait before showing a new item notification
        /// This allows us to group items together into one notification
        /// </summary>
        private const int NewItemDelay = 60000;

        /// <summary>
        /// The current new item timer
        /// </summary>
        /// <value>The new item timer.</value>
        private Timer NewItemTimer { get; set; }

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

            Kernel.Instance.ReloadCompleted += KernelReloadCompleted;
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
        /// Handles the LibraryChanged event of the Instance control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ChildrenChangedEventArgs" /> instance containing the event data.</param>
        void Instance_LibraryChanged(object sender, ChildrenChangedEventArgs e)
        {
            var newItems = e.ItemsAdded.Where(i => !i.IsFolder).ToList();

            // Use a timer to prevent lots of these notifications from showing in a short period of time
            if (newItems.Count > 0)
            {
                lock (_newlyAddedItems)
                {
                    _newlyAddedItems.AddRange(newItems);

                    if (NewItemTimer == null)
                    {
                        NewItemTimer = new Timer(NewItemTimerCallback, null, NewItemDelay, Timeout.Infinite);
                    }
                    else
                    {
                        NewItemTimer.Change(NewItemDelay, Timeout.Infinite);
                    }
                }
            }
        }

        /// <summary>
        /// Called when the new item timer expires
        /// </summary>
        /// <param name="state">The state.</param>
        private void NewItemTimerCallback(object state)
        {
            List<BaseItem> newItems;

            // Lock the list and release all resources
            lock (_newlyAddedItems)
            {
                newItems = _newlyAddedItems.ToList();
                _newlyAddedItems.Clear();

                NewItemTimer.Dispose();
                NewItemTimer = null;
            }

            // Show the notification
            if (newItems.Count == 1)
            {
                Dispatcher.InvokeAsync(() => MbTaskbarIcon.ShowCustomBalloon(new ItemUpdateNotification(_logger)
                {
                    DataContext = newItems[0]

                }, PopupAnimation.Slide, 6000));
            }
            else if (newItems.Count > 1)
            {
                Dispatcher.InvokeAsync(() => MbTaskbarIcon.ShowCustomBalloon(new MultiItemUpdateNotification(_logger)
                {
                    DataContext = newItems

                }, PopupAnimation.Slide, 6000));
            }
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
        /// Kernels the reload completed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void KernelReloadCompleted(object sender, EventArgs e)
        {
            Kernel.Instance.LibraryManager.LibraryChanged -= Instance_LibraryChanged;
            Kernel.Instance.LibraryManager.LibraryChanged += Instance_LibraryChanged;

            if (_appHost.IsFirstRun)
            {
                LaunchStartupWizard();
            }
        }

        /// <summary>
        /// Launches the startup wizard.
        /// </summary>
        private void LaunchStartupWizard()
        {
            var user = _appHost.Resolve<IUserManager>().Users.FirstOrDefault(u => u.Configuration.IsAdministrator);

            App.OpenDashboardPage("wizardStart.html", user);
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
