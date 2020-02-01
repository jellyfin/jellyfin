using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks
{
    /// <summary>
    /// Plugin Update Task.
    /// </summary>
    public class PluginUpdateTask : IScheduledTask, IConfigurableScheduledTask
    {
        /// <summary>
        /// The _logger.
        /// </summary>
        private readonly ILogger _logger;

        private readonly IInstallationManager _installationManager;

        public PluginUpdateTask(ILogger logger, IInstallationManager installationManager)
        {
            _logger = logger;
            _installationManager = installationManager;
        }

        /// <summary>
        /// Creates the triggers that define when the task will run.
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // At startup
            yield return new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerStartup };

            // Every so often
            yield return new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(24).Ticks };
        }

        /// <summary>
        /// Update installed plugins.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns><see cref="Task" />.</returns>
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            progress.Report(0);

            var packagesToInstall = await _installationManager.GetAvailablePluginUpdates(cancellationToken)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            progress.Report(10);

            var numComplete = 0;

            foreach (var package in packagesToInstall)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await _installationManager.InstallPackage(package, cancellationToken).ConfigureAwait(false);
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
                    _logger.LogError(ex, "Error downloading {0}", package.name);
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Error updating {0}", package.name);
                }

                // Update progress
                lock (progress)
                {
                    progress.Report((90.0 * ++numComplete / packagesToInstall.Count) + 10);
                }
            }

            progress.Report(100);
        }

        /// <inheritdoc />
        public string Name => "Update Plugins";

        /// <inheritdoc />
        public string Description => "Downloads and installs updates for plugins that are configured to update automatically.";

        /// <inheritdoc />
        public string Category => "Application";

        /// <inheritdoc />
        public string Key => "PluginUpdates";

        /// <inheritdoc />
        public bool IsHidden => false;

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public bool IsLogged => true;
    }
}
