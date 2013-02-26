using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.ScheduledTasks
{
    /// <summary>
    /// Class RefreshMediaLibraryTask
    /// </summary>
    public class RefreshMediaLibraryTask : IScheduledTask
    {
        /// <summary>
        /// The _kernel
        /// </summary>
        private readonly Kernel _kernel;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshMediaLibraryTask" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        public RefreshMediaLibraryTask(Kernel kernel)
        {
            _kernel = kernel;
        }

        /// <summary>
        /// Gets the default triggers.
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[] { 

                new StartupTrigger(),

                new SystemEventTrigger{ SystemEvent = SystemEvent.WakeFromSleep},

                new IntervalTrigger{ Interval = TimeSpan.FromHours(2)}
            };
        }

        /// <summary>
        /// Executes the internal.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(0);

            return _kernel.LibraryManager.ValidateMediaLibrary(progress, cancellationToken);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Scan media library"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return "Scans your media library and refreshes metatata based on configuration."; }
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public string Category
        {
            get
            {
                return "Library";
            }
        }
    }
}
