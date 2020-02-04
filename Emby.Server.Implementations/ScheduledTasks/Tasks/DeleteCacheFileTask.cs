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

namespace Emby.Server.Implementations.ScheduledTasks.Tasks
{
    /// <summary>
    /// Deletes old cache files
    /// </summary>
    public class DeleteCacheFileTask : IScheduledTask, IConfigurableScheduledTask
    {
        /// <summary>
        /// Gets or sets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        private IApplicationPaths ApplicationPaths { get; set; }

        private readonly ILogger _logger;

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteCacheFileTask" /> class.
        /// </summary>
        public DeleteCacheFileTask(IApplicationPaths appPaths, ILogger logger, IFileSystem fileSystem)
        {
            ApplicationPaths = appPaths;
            _logger = logger;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[] {

                // Every so often
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(24).Ticks}
            };
        }

        /// <summary>
        /// Returns the task to be executed
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var minDateModified = DateTime.UtcNow.AddDays(-30);

            try
            {
                DeleteCacheFilesFromDirectory(cancellationToken, ApplicationPaths.CachePath, minDateModified, progress);
            }
            catch (DirectoryNotFoundException)
            {
                // No biggie here. Nothing to delete
            }

            progress.Report(90);

            minDateModified = DateTime.UtcNow.AddDays(-1);

            try
            {
                DeleteCacheFilesFromDirectory(cancellationToken, ApplicationPaths.TempDirectory, minDateModified, progress);
            }
            catch (DirectoryNotFoundException)
            {
                // No biggie here. Nothing to delete
            }

            return Task.CompletedTask;
        }


        /// <summary>
        /// Deletes the cache files from directory with a last write time less than a given date
        /// </summary>
        /// <param name="cancellationToken">The task cancellation token.</param>
        /// <param name="directory">The directory.</param>
        /// <param name="minDateModified">The min date modified.</param>
        /// <param name="progress">The progress.</param>
        private void DeleteCacheFilesFromDirectory(CancellationToken cancellationToken, string directory, DateTime minDateModified, IProgress<double> progress)
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

        public string Name => "Clean Cache Directory";

        public string Description => "Deletes cache files no longer needed by the system.";

        public string Category => "Maintenance";

        public string Key => "DeleteCacheFiles";

        public bool IsHidden => false;

        public bool IsEnabled => true;

        public bool IsLogged => true;
    }
}
