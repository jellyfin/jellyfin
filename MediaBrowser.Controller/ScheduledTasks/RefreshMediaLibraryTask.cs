using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.ScheduledTasks
{
    /// <summary>
    /// Class RefreshMediaLibraryTask
    /// </summary>
    [Export(typeof(IScheduledTask))]
    public class RefreshMediaLibraryTask : BaseScheduledTask<Kernel>
    {
        /// <summary>
        /// Gets the default triggers.
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        protected override IEnumerable<BaseTaskTrigger> GetDefaultTriggers()
        {
            return new BaseTaskTrigger[] { 

                new StartupTrigger(Kernel),

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
        protected override Task ExecuteInternal(CancellationToken cancellationToken, IProgress<double> progress)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(0);

            return Kernel.LibraryManager.ValidateMediaLibrary(progress, cancellationToken);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Scan media library"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get { return "Scans your media library and refreshes metatata based on configuration."; }
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public override string Category
        {
            get
            {
                return "Library";
            }
        }
    }
}
