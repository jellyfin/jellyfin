using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.ScheduledTasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Implementations.ScheduledTasks.Tasks
{
    /// <summary>
    /// Deletes old log files
    /// </summary>
    public class DeleteLogFileTask : IScheduledTask
    {
        /// <summary>
        /// Gets or sets the configuration manager.
        /// </summary>
        /// <value>The configuration manager.</value>
        private IConfigurationManager ConfigurationManager { get; set; }

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteLogFileTask" /> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        public DeleteLogFileTask(IConfigurationManager configurationManager, IFileSystem fileSystem)
        {
            ConfigurationManager = configurationManager;
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
            
                // At startup
                new StartupTrigger (),

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
            // Delete log files more than n days old
            var minDateModified = DateTime.UtcNow.AddDays(-(ConfigurationManager.CommonConfiguration.LogFileRetentionDays));

            var filesToDelete = new DirectoryInfo(ConfigurationManager.CommonApplicationPaths.LogDirectoryPath).EnumerateFileSystemInfos("*", SearchOption.AllDirectories)
                          .Where(f => _fileSystem.GetLastWriteTimeUtc(f) < minDateModified)
                          .ToList();

            var index = 0;

            foreach (var file in filesToDelete)
            {
                double percent = index;
                percent /= filesToDelete.Count;

                progress.Report(100 * percent);

                cancellationToken.ThrowIfCancellationRequested();

                File.Delete(file.FullName);

                index++;
            }

            progress.Report(100);

            return Task.FromResult(true);
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Log file cleanup"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return string.Format("Deletes log files that are more than {0} days old.", ConfigurationManager.CommonConfiguration.LogFileRetentionDays); }
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
    }
}
