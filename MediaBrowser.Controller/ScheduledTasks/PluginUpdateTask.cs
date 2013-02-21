using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.ScheduledTasks
{
    /// <summary>
    /// Plugin Update Task
    /// </summary>
    [Export(typeof(IScheduledTask))]
    public class PluginUpdateTask : BaseScheduledTask<Kernel>
    {
        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        protected override IEnumerable<BaseTaskTrigger> GetDefaultTriggers()
        {
            return new BaseTaskTrigger[] { 
            
                // 1:30am
                new DailyTrigger { TimeOfDay = TimeSpan.FromHours(1.5) },

                new IntervalTrigger { Interval = TimeSpan.FromHours(2)}
            };
        }

        /// <summary>
        /// Update installed plugins
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        protected override async Task ExecuteInternal(CancellationToken cancellationToken, IProgress<TaskProgress> progress)
        {
            progress.Report(new TaskProgress { Description = "Checking for plugin updates", PercentComplete = 0 });

            var packagesToInstall = (await Kernel.InstallationManager.GetAvailablePluginUpdates(true, cancellationToken).ConfigureAwait(false)).ToList();

            progress.Report(new TaskProgress { PercentComplete = 10 });

            var numComplete = 0;

            // Create tasks for each one
            var tasks = packagesToInstall.Select(i => Task.Run(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await Kernel.InstallationManager.InstallPackage(i, new Progress<double> { }, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // InstallPackage has it's own inner cancellation token, so only throw this if it's ours
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                }
                catch (HttpException ex)
                {
                    Logger.ErrorException("Error downloading {0}", ex, i.name);
                }
                catch (IOException ex)
                {
                    Logger.ErrorException("Error updating {0}", ex, i.name);
                }

                // Update progress
                lock (progress)
                {
                    numComplete++;
                    double percent = numComplete;
                    percent /= packagesToInstall.Count;

                    progress.Report(new TaskProgress { PercentComplete = (90 * percent) + 10 });
                }
            }));

            cancellationToken.ThrowIfCancellationRequested();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            progress.Report(new TaskProgress { PercentComplete = 100 });
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Check for plugin updates"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get { return "Downloads and installs updates for plugins that are configured to update automatically."; }
        }
    }
}