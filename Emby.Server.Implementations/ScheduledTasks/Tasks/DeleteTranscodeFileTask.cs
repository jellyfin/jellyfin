using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks
{
    /// <summary>
    /// Deletes all transcoding temp files.
    /// </summary>
    public class DeleteTranscodeFileTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILogger<DeleteTranscodeFileTask> _logger;
        private readonly IConfigurationManager _configurationManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILocalizationManager _localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteTranscodeFileTask"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{DeleteTranscodeFileTask}"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        public DeleteTranscodeFileTask(
            ILogger<DeleteTranscodeFileTask> logger,
            IFileSystem fileSystem,
            IConfigurationManager configurationManager,
            ILocalizationManager localization)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
            _localization = localization;
        }

        /// <inheritdoc />
        public string Name => _localization.GetLocalizedString("TaskCleanTranscode");

        /// <inheritdoc />
        public string Description => _localization.GetLocalizedString("TaskCleanTranscodeDescription");

        /// <inheritdoc />
        public string Category => _localization.GetLocalizedString("TasksMaintenanceCategory");

        /// <inheritdoc />
        public string Key => "DeleteTranscodeFiles";

        /// <inheritdoc />
        public bool IsHidden => false;

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public bool IsLogged => true;

        /// <summary>
        /// Creates the triggers that define when the task will run.
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerInterval,
                    IntervalTicks = TimeSpan.FromHours(24).Ticks
                }
            };
        }

        /// <inheritdoc />
        public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var minDateModified = DateTime.UtcNow.AddDays(-1);
            progress.Report(50);

            DeleteTempFilesFromDirectory(_configurationManager.GetTranscodePath(), minDateModified, progress, cancellationToken);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Deletes the transcoded temp files from directory with a last write time less than a given date.
        /// </summary>
        /// <param name="directory">The directory.</param>
        /// <param name="minDateModified">The min date modified.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The task cancellation token.</param>
        private void DeleteTempFilesFromDirectory(string directory, DateTime minDateModified, IProgress<double> progress, CancellationToken cancellationToken)
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

                DeleteFile(file.FullName);

                index++;
            }

            DeleteEmptyFolders(directory);

            progress.Report(100);
        }

        private void DeleteEmptyFolders(string parent)
        {
            foreach (var directory in _fileSystem.GetDirectoryPaths(parent))
            {
                DeleteEmptyFolders(directory);
                if (!_fileSystem.GetFileSystemEntryPaths(directory).Any())
                {
                    try
                    {
                        Directory.Delete(directory, false);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogError(ex, "Error deleting directory {Path}", directory);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "Error deleting directory {Path}", directory);
                    }
                }
            }
        }

        private void DeleteFile(string path)
        {
            try
            {
                _fileSystem.DeleteFile(path);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Error deleting file {Path}", path);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error deleting file {Path}", path);
            }
        }
    }
}
