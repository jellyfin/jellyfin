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
    /// Class PeopleValidationTask
    /// </summary>
    [Export(typeof(IScheduledTask))]
    public class PeopleValidationTask : BaseScheduledTask<Kernel>
    {
        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        protected override IEnumerable<BaseTaskTrigger> GetDefaultTriggers()
        {
            return new BaseTaskTrigger[]
                {
                    new DailyTrigger { TimeOfDay = TimeSpan.FromHours(2) },

                    new IntervalTrigger{ Interval = TimeSpan.FromHours(12)}
                };
        }

        /// <summary>
        /// Returns the task to be executed
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        protected override Task ExecuteInternal(CancellationToken cancellationToken, IProgress<TaskProgress> progress)
        {
            return Kernel.LibraryManager.ValidatePeople(cancellationToken, progress);
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Refresh people"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get { return "Updates metadata for actors, artists and directors in your media library."; }
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
