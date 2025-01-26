using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks
{
    /// <summary>
    /// Plugin Update Task.
    /// </summary>
    public class PluginUpdateTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILogger<PluginUpdateTask> _logger;

        private readonly IInstallationManager _installationManager;
        private readonly ILocalizationManager _localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginUpdateTask" /> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="installationManager">Instance of the <see cref="IInstallationManager"/> interface.</param>
        /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        public PluginUpdateTask(ILogger<PluginUpdateTask> logger, IInstallationManager installationManager, ILocalizationManager localization)
        {
            _logger = logger;
            _installationManager = installationManager;
            _localization = localization;
        }

        /// <inheritdoc />
        public string Name => _localization.GetLocalizedString("TaskUpdatePlugins");

        /// <inheritdoc />
        public string Description => _localization.GetLocalizedString("TaskUpdatePluginsDescription");

        /// <inheritdoc />
        public string Category => _localization.GetLocalizedString("TasksApplicationCategory");

        /// <inheritdoc />
        public string Key => "PluginUpdates";

        /// <inheritdoc />
        public bool IsHidden => false;

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public bool IsLogged => true;

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // At startup
            yield return new TaskTriggerInfo { Type = TaskTriggerInfoType.StartupTrigger };

            // Every so often
            yield return new TaskTriggerInfo { Type = TaskTriggerInfoType.IntervalTrigger, IntervalTicks = TimeSpan.FromHours(24).Ticks };
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            progress.Report(0);

            var packageFetchTask = _installationManager.GetAvailablePluginUpdates(cancellationToken);
            var packagesToInstall = (await packageFetchTask.ConfigureAwait(false)).ToList();

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
                    // InstallPackage has its own inner cancellation token, so only throw this if it's ours
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Error downloading {0}", package.Name);
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Error updating {0}", package.Name);
                }
                catch (InvalidDataException ex)
                {
                    _logger.LogError(ex, "Error updating {0}", package.Name);
                }

                // Update progress
                lock (progress)
                {
                    progress.Report((90.0 * ++numComplete / packagesToInstall.Count) + 10);
                }
            }

            progress.Report(100);
        }
    }
}
