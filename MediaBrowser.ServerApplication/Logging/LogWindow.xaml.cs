using MediaBrowser.Common.Implementations.Logging;
using MediaBrowser.Model.Logging;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MediaBrowser.ServerApplication.Logging
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : Window
    {
        /// <summary>
        /// The _ui thread
        /// </summary>
        private readonly TaskScheduler _uiThread;

        private readonly ILogManager _logManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogWindow" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        public LogWindow(ILogManager logManager)
        {
            InitializeComponent();
            _uiThread = TaskScheduler.FromCurrentSynchronizationContext();
            _logManager = logManager;

            Loaded += LogWindow_Loaded;
        }

        /// <summary>
        /// Handles the Loaded event of the LogWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void LogWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ((NlogManager)_logManager).RemoveTarget("LogWindowTraceTarget");

            ((NlogManager)_logManager).AddLogTarget(new TraceTarget
            {
                Layout = "${longdate}, ${level}, ${logger}, ${message}",
                Name = "LogWindowTraceTarget"

            }, LogSeverity.Debug);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Window.Closing" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.ComponentModel.CancelEventArgs" /> that contains the event data.</param>
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            ((NlogManager) _logManager).RemoveTarget("LogWindowTraceTarget");
        }

        /// <summary>
        /// Logs the message.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        public async void LogMessage(string msg)
        {
            await Task.Factory.StartNew(() => lbxLogData.Items.Insert(0, msg.TrimEnd('\n')), CancellationToken.None, TaskCreationOptions.None, _uiThread);
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
        /// Adds the log target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="name">The name.</param>
        private void AddLogTarget(Target target, string name)
        {
            var config = NLog.LogManager.Configuration;

            target.Name = name;
            config.AddTarget(name, target);

            var level = LogLevel.Debug;

            var rule = new LoggingRule("*", level, target);
            config.LoggingRules.Add(rule);

            NLog.LogManager.Configuration = config;
        }
        
        /// <summary>
        /// Shuts down.
        /// </summary>
        public async void ShutDown()
        {
            await Task.Factory.StartNew(Close, CancellationToken.None, TaskCreationOptions.None, _uiThread);
        }

    }

}
