using System.Reflection;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Implementations.ScheduledTasks.Tasks
{
    /// <summary>
    /// Class ReloadLoggerFileTask
    /// </summary>
    public class StatisticsTask : IScheduledTask, IConfigurableScheduledTask
    {
        /// <summary>
        /// Gets or sets the log manager.
        /// </summary>
        /// <value>The log manager.</value>
        private ILogManager LogManager { get; set; }
        /// <summary>
        /// Gets or sets the app host
        /// </summary>
        /// <value>The application host.</value>
        private IApplicationHost ApplicationHost { get; set; }

        /// <summary>
        /// The network manager
        /// </summary>
        private INetworkManager NetworkManager { get; set; }

        /// <summary>
        /// The http client
        /// </summary>
        private IHttpClient HttpClient { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReloadLoggerFileTask" /> class.
        /// </summary>
        /// <param name="logManager">The logManager.</param>
        /// <param name="appHost"></param>
        /// <param name="httpClient"></param>
        public StatisticsTask(ILogManager logManager, IApplicationHost appHost, INetworkManager networkManager, IHttpClient httpClient)
        {
            LogManager = logManager;
            ApplicationHost = appHost;
            NetworkManager = networkManager;
            HttpClient = httpClient;
        }

        /// <summary>
        /// Gets the default triggers.
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            var trigger = new DailyTrigger { TimeOfDay = TimeSpan.FromHours(20) }; //8pm - when the system is most likely to be active
            var trigger2 = new StartupTrigger(); //and also at system start

            return new ITaskTrigger[] { trigger, trigger2 };
        }

        /// <summary>
        /// Executes the internal.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(0);
            var mac = NetworkManager.GetMacAddress();
            var data = new Dictionary<string, string> { { "feature", Assembly.GetExecutingAssembly().GetName().ToString() }, { "mac", mac }, { "ver", ApplicationHost.ApplicationVersion.ToString() }, { "platform", Environment.OSVersion.VersionString } };
            await HttpClient.Post(Constants.Constants.MbAdminUrl + "service/registration/ping", data, CancellationToken.None).ConfigureAwait(false);
            progress.Report(100);

        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Collect stats"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return "Pings the admin site just so we know how many folks are out there and what version they are on."; }
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public string Category
        {
            get { return "Application"; }
        }

        public bool IsHidden
        {
            get { return true; }
        }

        public bool IsEnabled
        {
            get { return true; }
        }
    }
}
