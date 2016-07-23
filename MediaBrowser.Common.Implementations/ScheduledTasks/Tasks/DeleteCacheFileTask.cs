using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Common.Implementations.ScheduledTasks.Tasks
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
        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            // Until we can vary these default triggers per server and MBT, we need something that makes sense for both
            return new ITaskTrigger[] { 
            
                // Every so often
                new IntervalTrigger { Interval = TimeSpan.FromHours(24)}
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

            return Task.FromResult(true);
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
                        _fileSystem.DeleteDirectory(directory, false);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.ErrorException("Error deleting directory {0}", ex, directory);
                    }
                    catch (IOException ex)
                    {
                        _logger.ErrorException("Error deleting directory {0}", ex, directory);
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
                _logger.ErrorException("Error deleting file {0}", ex, path);
            }
            catch (IOException ex)
            {
                _logger.ErrorException("Error deleting file {0}", ex, path);
            }
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Cache file cleanup"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return "Deletes cache files no longer needed by the system"; }
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public string Category
        {
            get
            {
                return "Maintenance";
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is hidden.
        /// </summary>
        /// <value><c>true</c> if this instance is hidden; otherwise, <c>false</c>.</value>
        public bool IsHidden
        {
            get { return true; }
        }

        public bool IsEnabled
        {
            get { return true; }
        }
    }
}
