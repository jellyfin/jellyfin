using MediaBrowser.Common.Kernel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.ScheduledTasks.Tasks
{
    /// <summary>
    /// Plugin Update Task
    /// </summary>
    [Export(typeof(IScheduledTask))]
    public class SystemUpdateTask : BaseScheduledTask<IKernel>
    {
        /// <summary>
        /// The _app host
        /// </summary>
        private readonly IApplicationHost _appHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemUpdateTask" /> class.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        [ImportingConstructor]
        public SystemUpdateTask([Import("appHost")] IApplicationHost appHost)
        {
            _appHost = appHost;
        }

        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        protected override IEnumerable<BaseTaskTrigger> GetDefaultTriggers()
        {
            return new BaseTaskTrigger[] { 
            
                // 1am
                new DailyTrigger { TimeOfDay = TimeSpan.FromHours(1) },

                new IntervalTrigger { Interval = TimeSpan.FromHours(2)}
            };
        }

        /// <summary>
        /// Returns the task to be executed
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        protected override async Task ExecuteInternal(CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (!_appHost.CanSelfUpdate) return;

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
                progress.Report(100);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (Kernel.Configuration.EnableAutoUpdate)
            {
                Logger.Info("Update Revision {0} available.  Updating...", updateInfo.AvailableVersion);

                innerProgressHandler = (sender, e) => progress.Report((e * .9) + .1);

                innerProgress = new Progress<double>();
                innerProgress.ProgressChanged += innerProgressHandler;

                await _appHost.UpdateApplication(cancellationToken, innerProgress).ConfigureAwait(false);

                // Release the event handler
                innerProgress.ProgressChanged -= innerProgressHandler;
                
                Kernel.OnApplicationUpdated(updateInfo.AvailableVersion);
            }
            else
            {
                Logger.Info("A new version of Media Browser is available.");
            }

            progress.Report(100);
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Check for application updates"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get { return "Downloads and installs application updates."; }
        }
    }
}
