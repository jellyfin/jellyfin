using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Globalization;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks
{
    /// <summary>
    /// Deletes all transcoding temp files
    /// </summary>
    public class DeleteTranscodeFileTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILogger _logger;
        private readonly IConfigurationManager _configurationManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILocalizationManager _localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteTranscodeFileTask" /> class.
        /// </summary>
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

        /// <summary>
        /// Creates the triggers that define when the task will run.
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => new List<TaskTriggerInfo>();

        /// <summary>
        /// Returns the task to be executed.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var minDateModified = DateTime.UtcNow.AddDays(-1);
            progress.Report(50);

            DeleteTempFilesFromDirectory(cancellationToken, _configurationManager.GetTranscodePath(), minDateModified, progress);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Deletes the transcoded temp files from directory with a last write time less than a given date.
        /// </summary>
        /// <param name="cancellationToken">The task cancellation token.</param>
        /// <param name="directory">The directory.</param>
        /// <param name="minDateModified">The min date modified.</param>
        /// <param name="progress">The progress.</param>
        private void DeleteTempFilesFromDirectory(CancellationToken cancellationToken, string directory, DateTime minDateModified, IProgress<double> progress)
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
                        _logger.LogError(ex, "Error deleting directory {path}", directory);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "Error deleting directory {path}", directory);
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
                _logger.LogError(ex, "Error deleting file {path}", path);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error deleting file {path}", path);
            }
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
        public bool IsEnabled => false;

        /// <inheritdoc />
        public bool IsLogged => true;
    }
}
