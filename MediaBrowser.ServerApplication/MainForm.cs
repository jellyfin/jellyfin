using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.ServerApplication.Logging;
using MediaBrowser.ServerApplication.Native;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace MediaBrowser.ServerApplication
{
    public partial class MainForm : Form
    {
        private readonly ILogger _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly ILogManager _logManager;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IDisplayPreferencesRepository _displayPreferencesManager;
        private readonly IItemRepository _itemRepository;
        private LogForm _logForm;

        public MainForm(ILogManager logManager, IServerApplicationHost appHost, IServerConfigurationManager configurationManager, IUserManager userManager, ILibraryManager libraryManager, IJsonSerializer jsonSerializer, IDisplayPreferencesRepository displayPreferencesManager, IItemRepository itemRepo)
        {
            InitializeComponent();

            _logger = logManager.GetLogger("MainWindow");
            _itemRepository = itemRepo;
            _appHost = appHost;
            _logManager = logManager;
            _configurationManager = configurationManager;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _jsonSerializer = jsonSerializer;
            _displayPreferencesManager = displayPreferencesManager;

            cmdExit.Click += cmdExit_Click;
            cmdRestart.Click += cmdRestart_Click;
            cmdLogWindow.Click += cmdLogWindow_Click;
            cmdConfigure.Click += cmdConfigure_Click;
            cmdCommunity.Click += cmdCommunity_Click;
            cmdBrowse.Click += cmdBrowse_Click;
            cmdLibraryExplorer.Click += cmdLibraryExplorer_Click;

            cmdSwagger.Click += cmdSwagger_Click;
            cmdStandardDocs.Click += cmdStandardDocs_Click;
            cmdGtihub.Click += cmdGtihub_Click;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            LoadLogWindow(null, EventArgs.Empty);
            _logManager.LoggerLoaded += LoadLogWindow;
            _configurationManager.ConfigurationUpdated += Instance_ConfigurationUpdated;

            if (_appHost.IsFirstRun)
            {
                Action action = () => notifyIcon1.ShowBalloonTip(5000, "Media Browser", "Welcome to Media Browser Server!", ToolTipIcon.Info);

                Invoke(action);
            }
        }

        /// <summary>
        /// Handles the ConfigurationUpdated event of the Instance control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void Instance_ConfigurationUpdated(object sender, EventArgs e)
        {
            Action action = () =>
            {
                var isLogWindowOpen = _logForm != null;

                if ((!isLogWindowOpen && _configurationManager.Configuration.ShowLogWindow) ||
                    (isLogWindowOpen && !_configurationManager.Configuration.ShowLogWindow))
                {
                    _logManager.ReloadLogger(_configurationManager.Configuration.EnableDebugLevelLogging
                        ? LogSeverity.Debug
                        : LogSeverity.Info);
                }
            };

            Invoke(action);
        }

        /// <summary>
        /// Loads the log window.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        void LoadLogWindow(object sender, EventArgs args)
        {
            CloseLogWindow();

            Action action = () =>
            {
                // Add our log window if specified
                if (_configurationManager.Configuration.ShowLogWindow)
                {
                    _logForm = new LogForm(_logManager);

                    Trace.Listeners.Add(new WindowTraceListener(_logForm));
                }
                else
                {
                    Trace.Listeners.Remove("MBLogWindow");
                }
                
                // Set menu option indicator
                cmdLogWindow.Checked = _configurationManager.Configuration.ShowLogWindow;
            };

            Invoke(action);
        }

        /// <summary>
        /// Closes the log window.
        /// </summary>
        void CloseLogWindow()
        {
            if (_logForm != null)
            {
                _logForm.ShutDown();
            }
        }

        void cmdBrowse_Click(object sender, EventArgs e)
        {
            BrowserLauncher.OpenWebClient(_userManager, _configurationManager, _appHost, _logger);
        }

        void cmdCommunity_Click(object sender, EventArgs e)
        {
            BrowserLauncher.OpenCommunity(_logger);
        }

        void cmdConfigure_Click(object sender, EventArgs e)
        {
            BrowserLauncher.OpenDashboard(_userManager, _configurationManager, _appHost, _logger);
        }

        void cmdLogWindow_Click(object sender, EventArgs e)
        {
            _configurationManager.Configuration.ShowLogWindow = !_configurationManager.Configuration.ShowLogWindow;
            _configurationManager.SaveConfiguration();
            LoadLogWindow(sender, e);
        }

        void cmdLibraryExplorer_Click(object sender, EventArgs e)
        {
            new LibraryViewer(_jsonSerializer, _userManager, _libraryManager, _displayPreferencesManager, _itemRepository).Show();
        }

        void cmdRestart_Click(object sender, EventArgs e)
        {
            _appHost.Restart();
        }

        void cmdExit_Click(object sender, EventArgs e)
        {
            Close();
            _appHost.Shutdown();
        }

        void cmdGtihub_Click(object sender, EventArgs e)
        {
            BrowserLauncher.OpenGithub(_logger);
        }

        void cmdStandardDocs_Click(object sender, EventArgs e)
        {
            BrowserLauncher.OpenStandardApiDocumentation(_configurationManager, _appHost, _logger);
        }

        void cmdSwagger_Click(object sender, EventArgs e)
        {
            BrowserLauncher.OpenSwagger(_configurationManager, _appHost, _logger); 
        }
    }
}
