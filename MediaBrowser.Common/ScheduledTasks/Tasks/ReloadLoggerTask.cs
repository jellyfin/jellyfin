using MediaBrowser.Common.Kernel;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.ScheduledTasks.Tasks
{
    /// <summary>
    /// Class ReloadLoggerFileTask
    /// </summary>
    public class ReloadLoggerFileTask : BaseScheduledTask<IKernel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReloadLoggerFileTask" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="logger">The logger.</param>
        public ReloadLoggerFileTask(IKernel kernel, ITaskManager taskManager, ILogger logger)
            : base(kernel, taskManager, logger)
        {
        }

        /// <summary>
        /// Gets the default triggers.
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        protected override IEnumerable<BaseTaskTrigger> GetDefaultTriggers()
        {
            var trigger = new DailyTrigger { TimeOfDay = TimeSpan.FromHours(0) }; //12am

            return new[] { trigger };
        }

        /// <summary>
        /// Executes the internal.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        protected override Task ExecuteInternal(CancellationToken cancellationToken, IProgress<double> progress)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(0);
            
            return Task.Run(() => Kernel.ReloadLogger());
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Start new log file"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get { return "Moves logging to a new file to help reduce log file sizes."; }
        }
    }
}
