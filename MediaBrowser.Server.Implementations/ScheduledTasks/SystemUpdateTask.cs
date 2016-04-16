using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.ScheduledTasks
{
    /// <summary>
    /// Plugin Update Task
    /// </summary>
    public class SystemUpdateTask : IScheduledTask, IHasKey
    {
        /// <summary>
        /// The _app host
        /// </summary>
        private readonly IApplicationHost _appHost;

        /// <summary>
        /// Gets or sets the configuration manager.
        /// </summary>
        /// <value>The configuration manager.</value>
        private IConfigurationManager ConfigurationManager { get; set; }
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemUpdateTask" /> class.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="logger">The logger.</param>
        public SystemUpdateTask(IApplicationHost appHost, IConfigurationManager configurationManager, ILogger logger)
        {
            _appHost = appHost;
            ConfigurationManager = configurationManager;
            Logger = logger;
        }

        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            // Until we can vary these default triggers per server and MBT, we need something that makes sense for both
            return new ITaskTrigger[] { 
            
                // At startup
                new StartupTrigger(),

                // Every so often
                new IntervalTrigger { Interval = TimeSpan.FromHours(24)}
            };
        }

        /// <summary>
        /// Returns the task to be executed
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            EventHandler<double> innerProgressHandler = (sender, e) => progress.Report(e * .1);

            // Create a progress object for the update check
            var innerProgress = new Progress<double>();
            innerProgress.ProgressChanged += innerProgressHandler;

            var updateInfo = await _appHost.CheckForApplicationUpdate(cancellationToken, innerProgress).ConfigureAwait(false);

            // Release the event handler
            innerProgress.ProgressChanged -= innerProgressHandler;

            progress.Report(10);

            if (!updateInfo.IsUpdateAvailable)
            {
                Logger.Debug("No application update available.");
                progress.Report(100);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!_appHost.CanSelfUpdate) return;

            if (ConfigurationManager.CommonConfiguration.EnableAutoUpdate)
            {
                Logger.Info("Update Revision {0} available.  Updating...", updateInfo.AvailableVersion);

                innerProgressHandler = (sender, e) => progress.Report(e * .9 + .1);

                innerProgress = new Progress<double>();
                innerProgress.ProgressChanged += innerProgressHandler;

                await _appHost.UpdateApplication(updateInfo.Package, cancellationToken, innerProgress).ConfigureAwait(false);

                // Release the event handler
                innerProgress.ProgressChanged -= innerProgressHandler;
            }
            else
            {
                Logger.Info("A new version of " + _appHost.Name + " is available.");
            }

            progress.Report(100);
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Check for application updates"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return "Downloads and installs application updates."; }
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public string Category
        {
            get { return "Application"; }
        }

        public string Key
        {
            get { return "SystemUpdateTask"; }
        }
    }
}
