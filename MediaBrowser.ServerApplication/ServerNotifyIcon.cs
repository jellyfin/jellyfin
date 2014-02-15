using System;
using System.Diagnostics;
using System.Windows.Forms;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.ServerApplication.Logging;
using MediaBrowser.ServerApplication.Native;

namespace MediaBrowser.ServerApplication
{
    public class ServerNotifyIcon : IDisposable
    {
        bool IsDisposing = false;
        
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem cmdExit;
        private System.Windows.Forms.ToolStripMenuItem cmdBrowse;
        private System.Windows.Forms.ToolStripMenuItem cmdConfigure;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem cmdLibraryExplorer;
        private System.Windows.Forms.ToolStripMenuItem cmdRestart;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem cmdLogWindow;
        private System.Windows.Forms.ToolStripMenuItem cmdCommunity;
        private System.Windows.Forms.ToolStripMenuItem cmdApiDocs;
        private System.Windows.Forms.ToolStripMenuItem cmdStandardDocs;
        private System.Windows.Forms.ToolStripMenuItem cmdSwagger;
        private System.Windows.Forms.ToolStripMenuItem cmdGtihub;

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

        public bool Visible
        {
            get
            {
                return notifyIcon1.Visible;
            }
            set
            {
                Action act = () => notifyIcon1.Visible = false;
                contextMenuStrip1.Invoke(act);
            }
        }

        public ServerNotifyIcon(ILogManager logManager, IServerApplicationHost appHost, IServerConfigurationManager configurationManager, IUserManager userManager, ILibraryManager libraryManager, IJsonSerializer jsonSerializer, IDisplayPreferencesRepository displayPreferencesManager, IItemRepository itemRepo)
        {
            _logger = logManager.GetLogger("MainWindow");
            _itemRepository = itemRepo;
            _appHost = appHost;
            _logManager = logManager;
            _configurationManager = configurationManager;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _jsonSerializer = jsonSerializer;
            _displayPreferencesManager = displayPreferencesManager;
            
            var components = new System.ComponentModel.Container();
            
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(components);
            notifyIcon1 = new System.Windows.Forms.NotifyIcon(components);
            
            cmdExit = new System.Windows.Forms.ToolStripMenuItem();
            cmdCommunity = new System.Windows.Forms.ToolStripMenuItem();
            cmdLogWindow = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            cmdRestart = new System.Windows.Forms.ToolStripMenuItem();
            cmdLibraryExplorer = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            cmdConfigure = new System.Windows.Forms.ToolStripMenuItem();
            cmdBrowse = new System.Windows.Forms.ToolStripMenuItem();
            cmdApiDocs = new System.Windows.Forms.ToolStripMenuItem();
            cmdStandardDocs = new System.Windows.Forms.ToolStripMenuItem();
            cmdSwagger = new System.Windows.Forms.ToolStripMenuItem();
            cmdGtihub = new System.Windows.Forms.ToolStripMenuItem();
            
            // 
            // notifyIcon1
            // 
            notifyIcon1.ContextMenuStrip = contextMenuStrip1;
            notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            notifyIcon1.Text = "Media Browser";
            notifyIcon1.Visible = true;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            cmdBrowse,
            cmdConfigure,
            toolStripSeparator2,
            cmdLibraryExplorer,
            cmdRestart,
            toolStripSeparator1,
            cmdApiDocs,
            cmdLogWindow,
            cmdCommunity,
            cmdExit});
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.ShowCheckMargin = true;
            contextMenuStrip1.ShowImageMargin = false;
            contextMenuStrip1.Size = new System.Drawing.Size(209, 214);
            // 
            // cmdExit
            // 
            cmdExit.Name = "cmdExit";
            cmdExit.Size = new System.Drawing.Size(208, 22);
            cmdExit.Text = "Exit";
            // 
            // cmdCommunity
            // 
            cmdCommunity.Name = "cmdCommunity";
            cmdCommunity.Size = new System.Drawing.Size(208, 22);
            cmdCommunity.Text = "Visit Community";
            // 
            // cmdLogWindow
            // 
            cmdLogWindow.CheckOnClick = true;
            cmdLogWindow.Name = "cmdLogWindow";
            cmdLogWindow.Size = new System.Drawing.Size(208, 22);
            cmdLogWindow.Text = "Show Log Window";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(205, 6);
            // 
            // cmdRestart
            // 
            cmdRestart.Name = "cmdRestart";
            cmdRestart.Size = new System.Drawing.Size(208, 22);
            cmdRestart.Text = "Restart Server";
            // 
            // cmdLibraryExplorer
            // 
            cmdLibraryExplorer.Name = "cmdLibraryExplorer";
            cmdLibraryExplorer.Size = new System.Drawing.Size(208, 22);
            cmdLibraryExplorer.Text = "Open Library Explorer";
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new System.Drawing.Size(205, 6);
            // 
            // cmdConfigure
            // 
            cmdConfigure.Name = "cmdConfigure";
            cmdConfigure.Size = new System.Drawing.Size(208, 22);
            cmdConfigure.Text = "Configure Media Browser";
            // 
            // cmdBrowse
            // 
            cmdBrowse.Name = "cmdBrowse";
            cmdBrowse.Size = new System.Drawing.Size(208, 22);
            cmdBrowse.Text = "Browse Library";
            // 
            // cmdApiDocs
            // 
            cmdApiDocs.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            cmdStandardDocs,
            cmdSwagger,
            cmdGtihub});
            cmdApiDocs.Name = "cmdApiDocs";
            cmdApiDocs.Size = new System.Drawing.Size(208, 22);
            cmdApiDocs.Text = "View Api Documentation";
            // 
            // cmdStandardDocs
            // 
            cmdStandardDocs.Name = "cmdStandardDocs";
            cmdStandardDocs.Size = new System.Drawing.Size(136, 22);
            cmdStandardDocs.Text = "Standard";
            // 
            // cmdSwagger
            // 
            cmdSwagger.Name = "cmdSwagger";
            cmdSwagger.Size = new System.Drawing.Size(136, 22);
            cmdSwagger.Text = "Swagger";
            // 
            // cmdGtihub
            // 
            cmdGtihub.Name = "cmdGtihub";
            cmdGtihub.Size = new System.Drawing.Size(136, 22);
            cmdGtihub.Text = "Github Wiki";

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

            LoadLogWindow(null, EventArgs.Empty);
            _logManager.LoggerLoaded += LoadLogWindow;
            _configurationManager.ConfigurationUpdated += Instance_ConfigurationUpdated;

            if (_appHost.IsFirstRun)
            {
                Action action = () => notifyIcon1.ShowBalloonTip(5000, "Media Browser", "Welcome to Media Browser Server!", ToolTipIcon.Info);

                contextMenuStrip1.Invoke(action);
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

            contextMenuStrip1.Invoke(action);
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

            contextMenuStrip1.Invoke(action);
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

        ~ServerNotifyIcon()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!IsDisposing)
            {
                IsDisposing = true;
            }
        }
    }
}
