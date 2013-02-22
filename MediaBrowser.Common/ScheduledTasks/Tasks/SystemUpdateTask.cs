using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Deployment.Application;
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
        protected override async Task ExecuteInternal(CancellationToken cancellationToken, IProgress<TaskProgress> progress)
        {
            if (!ApplicationDeployment.IsNetworkDeployed) return;

            EventHandler<TaskProgress> innerProgressHandler = (sender, e) => progress.Report(new TaskProgress { PercentComplete = e.PercentComplete * .1 });

            // Create a progress object for the update check
            var innerProgress = new Progress<TaskProgress>();
            innerProgress.ProgressChanged += innerProgressHandler;

            var updateInfo = await new ApplicationUpdateCheck().CheckForApplicationUpdate(cancellationToken, innerProgress).ConfigureAwait(false);

            // Release the event handler
            innerProgress.ProgressChanged -= innerProgressHandler;

            progress.Report(new TaskProgress { PercentComplete = 10 });

            if (!updateInfo.UpdateAvailable)
            {
                progress.Report(new TaskProgress { PercentComplete = 100 });
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (Kernel.Configuration.EnableAutoUpdate)
            {
                Logger.Info("Update Revision {0} available.  Updating...", updateInfo.AvailableVersion);

                innerProgressHandler = (sender, e) => progress.Report(new TaskProgress { PercentComplete = (e.PercentComplete * .9) + .1 });

                innerProgress = new Progress<TaskProgress>();
                innerProgress.ProgressChanged += innerProgressHandler;

                await new ApplicationUpdater().UpdateApplication(cancellationToken, innerProgress).ConfigureAwait(false);

                // Release the event handler
                innerProgress.ProgressChanged -= innerProgressHandler;
                
                Kernel.OnApplicationUpdated(updateInfo.AvailableVersion);
            }
            else
            {
                Logger.Info("A new version of Media Browser is available.");
            }

            progress.Report(new TaskProgress { PercentComplete = 100 });
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
