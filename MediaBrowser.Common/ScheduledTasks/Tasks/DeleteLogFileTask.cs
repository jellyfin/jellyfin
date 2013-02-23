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
    /// Deletes old log files
    /// </summary>
    public class DeleteLogFileTask : BaseScheduledTask<IKernel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteLogFileTask" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="logger">The logger.</param>
        public DeleteLogFileTask(IKernel kernel, ITaskManager taskManager, ILogger logger)
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
                // Delete log files more than n days old
                var minDateModified = DateTime.UtcNow.AddDays(-(Kernel.Configuration.LogFileRetentionDays));

                var filesToDelete = new DirectoryInfo(Kernel.ApplicationPaths.LogDirectoryPath).EnumerateFileSystemInfos("*", SearchOption.AllDirectories)
                              .Where(f => f.LastWriteTimeUtc < minDateModified)
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
            });
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Log file cleanup"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get { return string.Format("Deletes log files that are more than {0} days old.", Kernel.Configuration.LogFileRetentionDays); }
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
