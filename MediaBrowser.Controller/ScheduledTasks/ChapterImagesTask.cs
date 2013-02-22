using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.ScheduledTasks
{
    [Export(typeof(IScheduledTask))]
    class ChapterImagesTask : BaseScheduledTask<Kernel>
    {
        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        protected override IEnumerable<BaseTaskTrigger> GetDefaultTriggers()
        {
            return new BaseTaskTrigger[]
                {
                    new DailyTrigger { TimeOfDay = TimeSpan.FromHours(4) }
                };
        }

        /// <summary>
        /// Returns the task to be executed
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        protected override Task ExecuteInternal(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var videos = Kernel.RootFolder.RecursiveChildren.OfType<Video>().Where(v => v.Chapters != null).ToList();

            var numComplete = 0;

            var tasks = videos.Select(v => Task.Run(async () =>
            {
                try
                {
                    await Kernel.FFMpegManager.PopulateChapterImages(v, cancellationToken, true, true);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error creating chapter images for {0}", ex, v.Name);
                }
                finally
                {
                    lock (progress)
                    {
                        numComplete++;
                        double percent = numComplete;
                        percent /= videos.Count;

                        progress.Report(100 * percent);
                    }
                }
            }));

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Create video chapter thumbnails"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get { return "Creates thumbnails for videos that have chapters."; }
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
