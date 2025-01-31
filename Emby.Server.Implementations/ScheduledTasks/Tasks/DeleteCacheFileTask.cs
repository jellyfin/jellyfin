using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks
{
    /// <summary>
    /// Deletes old cache files.
    /// </summary>
    public class DeleteCacheFileTask : IScheduledTask, IConfigurableScheduledTask
    {
        /// <summary>
        /// Gets or sets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        private readonly IApplicationPaths _applicationPaths;
        private readonly ILogger<DeleteCacheFileTask> _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ILocalizationManager _localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteCacheFileTask" /> class.
        /// </summary>
        /// <param name="appPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        public DeleteCacheFileTask(
            IApplicationPaths appPaths,
            ILogger<DeleteCacheFileTask> logger,
            IFileSystem fileSystem,
            ILocalizationManager localization)
        {
            _applicationPaths = appPaths;
            _logger = logger;
            _fileSystem = fileSystem;
            _localization = localization;
        }

        /// <inheritdoc />
        public string Name => _localization.GetLocalizedString("TaskCleanCache");

        /// <inheritdoc />
        public string Description => _localization.GetLocalizedString("TaskCleanCacheDescription");

        /// <inheritdoc />
        public string Category => _localization.GetLocalizedString("TasksMaintenanceCategory");

        /// <inheritdoc />
        public string Key => "DeleteCacheFiles";

        /// <inheritdoc />
        public bool IsHidden => false;

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public bool IsLogged => true;

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return
            [
                // Every so often
                new TaskTriggerInfo { Type = TaskTriggerInfoType.IntervalTrigger, IntervalTicks = TimeSpan.FromHours(24).Ticks }
            ];
        }

        /// <inheritdoc />
        public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var minDateModified = DateTime.UtcNow.AddDays(-30);

            try
            {
                DeleteCacheFilesFromDirectory(_applicationPaths.CachePath, minDateModified, progress, cancellationToken);
            }
            catch (DirectoryNotFoundException)
            {
                // No biggie here. Nothing to delete
            }

            progress.Report(90);

            minDateModified = DateTime.UtcNow.AddDays(-1);

            try
            {
                DeleteCacheFilesFromDirectory(_applicationPaths.TempDirectory, minDateModified, progress, cancellationToken);
            }
            catch (DirectoryNotFoundException)
            {
                // No biggie here. Nothing to delete
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Deletes the cache files from directory with a last write time less than a given date.
        /// </summary>
        /// <param name="directory">The directory.</param>
        /// <param name="minDateModified">The min date modified.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The task cancellation token.</param>
        private void DeleteCacheFilesFromDirectory(string directory, DateTime minDateModified, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var filesToDelete = _fileSystem.GetFiles(directory, true)
                .Where(f => _fileSystem.GetLastWriteTimeUtc(f) < minDateModified)
                .ToList();

            var index = 0;

            foreach (var file in filesToDelete)
            {
                double percent = index;
                percent /= filesToDelete.Count;

                progress.Report(100 * percent);

                cancellationToken.ThrowIfCancellationRequested();

                FileSystemHelper.DeleteFile(_fileSystem, file.FullName, _logger);

                index++;
            }

            FileSystemHelper.DeleteEmptyFolders(_fileSystem, directory, _logger);

            progress.Report(100);
        }
    }
}
