using MediaBrowser.Common.Implementations.Logging;
using MediaBrowser.Model.Logging;
using NLog.Targets;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaBrowser.ServerApplication.Logging
{
    public partial class LogForm : Form
    {
        private readonly TaskScheduler _uiThread;
        private readonly ILogManager _logManager;
        
        public LogForm(ILogManager logManager)
        {
            InitializeComponent();

            _logManager = logManager;
            _uiThread = TaskScheduler.FromCurrentSynchronizationContext();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            ((NlogManager)_logManager).RemoveTarget("LogWindowTraceTarget");

            ((NlogManager)_logManager).AddLogTarget(new TraceTarget
            {
                Layout = "${longdate}, ${level}, ${logger}, ${message}",
                Name = "LogWindowTraceTarget"

            }, LogSeverity.Debug);
        }

        /// <summary>
        /// Logs the message.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        public async void LogMessage(string msg)
        {
            await Task.Factory.StartNew(() =>
            {
                if (listBox1.Items.Count > 10000)
                {
                    //I think the quickest and safest thing to do here is just clear it out
                    listBox1.Items.Clear();
                }

                foreach (var line in msg.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        listBox1.Items.Insert(0, line);
                    }
                }

            }, CancellationToken.None, TaskCreationOptions.None, _uiThread);
        }

        /// <summary>
        /// The log layout
        /// </summary>
        /// <value>The log layout.</value>
        public string LogLayout
        {
            get { return "${longdate}, ${level}, ${logger}, ${message}"; }
        }

        /// <summary>
        /// Shuts down.
        /// </summary>
        public async void ShutDown()
        {
            await Task.Factory.StartNew(Close, CancellationToken.None, TaskCreationOptions.None, _uiThread);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            ((NlogManager)_logManager).RemoveTarget("LogWindowTraceTarget");
        }
    }
}
