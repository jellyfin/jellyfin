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
using MediaBrowser.Model.Globalization;

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
        private readonly ILocalizationManager _localization;

        public PluginUpdateTask(ILogger<PluginUpdateTask> logger, IInstallationManager installationManager)
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

        public string Name => _localization.GetLocalizedString("TaskUpdatePlugins");

        public string Description => _localization.GetLocalizedString("TaskUpdatePluginsDescription");

        public string Category => _localization.GetLocalizedString("TasksApplication");

        public string Key => "PluginUpdates";

        public bool IsHidden => false;

        public bool IsEnabled => true;

        public bool IsLogged => true;
    }
}
