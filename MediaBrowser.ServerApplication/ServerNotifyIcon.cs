using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Startup.Common.Browser;
using System;
using System.Windows.Forms;

namespace MediaBrowser.ServerApplication
{
    public class ServerNotifyIcon : IDisposable
    {
        bool IsDisposing = false;

        private NotifyIcon notifyIcon1;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem cmdExit;
        private ToolStripMenuItem cmdBrowse;
        private ToolStripMenuItem cmdConfigure;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem cmdRestart;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem cmdCommunity;

        private readonly ILogger _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly ILocalizationManager _localization;

        public bool Visible
        {
            get
            {
                return notifyIcon1.Visible;
            }
            set
            {
                Action act = () => notifyIcon1.Visible = false;
                Invoke(act);
            }
        }

        public void Invoke(Action action)
        {
            contextMenuStrip1.Invoke(action);
        }

        public ServerNotifyIcon(ILogManager logManager,
            IServerApplicationHost appHost,
            IServerConfigurationManager configurationManager,
            ILocalizationManager localization)
        {
            _logger = logManager.GetLogger("MainWindow");
            _localization = localization;
            _appHost = appHost;
            _configurationManager = configurationManager;

            var components = new System.ComponentModel.Container();

            var resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            contextMenuStrip1 = new ContextMenuStrip(components);
            notifyIcon1 = new NotifyIcon(components);

            cmdExit = new ToolStripMenuItem();
            cmdCommunity = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            cmdRestart = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            cmdConfigure = new ToolStripMenuItem();
            cmdBrowse = new ToolStripMenuItem();

            // 
            // notifyIcon1
            // 
            notifyIcon1.ContextMenuStrip = contextMenuStrip1;
            notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            notifyIcon1.Text = "Emby";
            notifyIcon1.Visible = true;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] {
            cmdBrowse,
            cmdConfigure,
            toolStripSeparator2,
            cmdRestart,
            toolStripSeparator1,
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
            // 
            // cmdCommunity
            // 
            cmdCommunity.Name = "cmdCommunity";
            cmdCommunity.Size = new System.Drawing.Size(208, 22);
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
            // 
            // cmdBrowse
            // 
            cmdBrowse.Name = "cmdBrowse";
            cmdBrowse.Size = new System.Drawing.Size(208, 22);

            cmdExit.Click += cmdExit_Click;
            cmdRestart.Click += cmdRestart_Click;
            cmdConfigure.Click += cmdConfigure_Click;
            cmdCommunity.Click += cmdCommunity_Click;
            cmdBrowse.Click += cmdBrowse_Click;

            _configurationManager.ConfigurationUpdated += Instance_ConfigurationUpdated;

            LocalizeText();

            notifyIcon1.DoubleClick += notifyIcon1_DoubleClick;
            Application.ThreadExit += Application_ThreadExit;
            Application.ApplicationExit += Application_ApplicationExit;
        }

        void Application_ThreadExit(object sender, EventArgs e)
        {
            try
            {
                notifyIcon1.Visible = false;
            }
            catch
            {

            }
        }

        void Application_ApplicationExit(object sender, EventArgs e)
        {
            try
            {
                notifyIcon1.Visible = false;
            }
            catch
            {

            }
        }

        void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            BrowserLauncher.OpenDashboard(_appHost);
        }

        private void LocalizeText()
        {
            _uiCulture = _configurationManager.Configuration.UICulture;

            cmdExit.Text = _localization.GetLocalizedString("LabelExit");
            cmdCommunity.Text = _localization.GetLocalizedString("LabelVisitCommunity");
            cmdBrowse.Text = _localization.GetLocalizedString("LabelBrowseLibrary");
            cmdConfigure.Text = _localization.GetLocalizedString("LabelConfigureServer");
            cmdRestart.Text = _localization.GetLocalizedString("LabelRestartServer");
        }

        private string _uiCulture;
        /// <summary>
        /// Handles the ConfigurationUpdated event of the Instance control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void Instance_ConfigurationUpdated(object sender, EventArgs e)
        {
            if (!string.Equals(_configurationManager.Configuration.UICulture, _uiCulture,
                    StringComparison.OrdinalIgnoreCase))
            {
                LocalizeText();
            }
        }

        void cmdBrowse_Click(object sender, EventArgs e)
        {
            BrowserLauncher.OpenWebClient(_appHost);
        }

        void cmdCommunity_Click(object sender, EventArgs e)
        {
            BrowserLauncher.OpenCommunity(_appHost);
        }

        void cmdConfigure_Click(object sender, EventArgs e)
        {
            BrowserLauncher.OpenDashboard(_appHost);
        }

        void cmdRestart_Click(object sender, EventArgs e)
        {
            _appHost.Restart();
        }

        void cmdExit_Click(object sender, EventArgs e)
        {
            _appHost.Shutdown();
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