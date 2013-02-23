using MediaBrowser.Common.Kernel;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.ScheduledTasks.Tasks
{
    /// <summary>
    /// Deletes old cache files
    /// </summary>
    public class DeleteCacheFileTask : BaseScheduledTask<IKernel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteCacheFileTask" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="logger">The logger.</param>
        public DeleteCacheFileTask(IKernel kernel, ITaskManager taskManager, ILogger logger)
            : base(kernel, taskManager, logger)
        {
        }

        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        protected override IEnumerable<BaseTaskTrigger> GetDefaultTriggers()
        {
            var trigger = new DailyTrigger { TimeOfDay = TimeSpan.FromHours(2) }; //2am

            return new[] { trigger };
        }

        /// <summary>
        /// Returns the task to be executed
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        protected override Task ExecuteInternal(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return Task.Run(() =>
            {
                var minDateModified = DateTime.UtcNow.AddMonths(-2);

                DeleteCacheFilesFromDirectory(cancellationToken, Kernel.ApplicationPaths.CachePath, minDateModified, progress);
            });
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
            var filesToDelete = new DirectoryInfo(directory).EnumerateFileSystemInfos("*", SearchOption.AllDirectories)
                .Where(f => !f.Attributes.HasFlag(FileAttributes.Directory) && f.LastWriteTimeUtc < minDateModified)
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
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Cache file cleanup"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get { return "Deletes cache files no longer needed by the system"; }
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public override string Category
        {
            get
            {
                return "Maintenance";
            }
        }
    }
}
